using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum TeamGenerationMethod
{
    GeneticAlgorithm,
    EvolutionaryStrategy
}

public enum PokemonRole
{
    Offensive,
    Defensive,
    Fast
}

[Serializable]
public class PokemonTeamGenerationConfig
{
    [Header("Generated Content")]
    [Min(1)] public int teamSize = 6;

    [Header("Power Target")]
    public int minAverageBST = 450;
    public int maxAverageBST = 500;

    [Tooltip("Distance outside the target interval at which Power Score becomes 0.")]
    [Min(1f)] public float bstPenaltyRange = 150f;

    [Header("Diversity Target")]
    [Range(1, 12)] public int targetUniqueTypes = 8;

    [Header("Output")]
    [Range(0f, 1f)] public float outputFitnessThreshold = 0.90f;
    [Min(1)] public int fallbackTopCandidates = 5;
}

[Serializable]
public class PokemonTeamCandidate
{
    /*
     * ============================================================
     * REPRESENTACIÓN DE UN INDIVIDUO
     * ============================================================
     *
     * Si teamSize = 6:
     *
     * genes = [25, 131, 94, 212, 135, 445]
     * genes.Length = 6
     *
     * Cada número corresponde a una posición dentro del dataset.
     *
     * Ejemplo conceptual:
     * dataset[25]  -> Pikachu
     * dataset[131] -> Lapras
     * dataset[94]  -> Gengar
     *
     * Por lo tanto, genes representa un equipo de seis Pokémon.
     */
    public int[] genes;

    public float fitness;
    public float powerScore;
    public float diversityScore;

    public float averageBST;
    public int uniquePokemon;
    public int uniqueTypes;
    public int uniqueRoles;

    public PokemonTeamCandidate(int teamSize)
    {
        genes = new int[teamSize];
    }

    public PokemonTeamCandidate Clone()
    {
        PokemonTeamCandidate copy = new PokemonTeamCandidate(genes.Length);
        Array.Copy(genes, copy.genes, genes.Length);

        copy.fitness = fitness;
        copy.powerScore = powerScore;
        copy.diversityScore = diversityScore;
        copy.averageBST = averageBST;
        copy.uniquePokemon = uniquePokemon;
        copy.uniqueTypes = uniqueTypes;
        copy.uniqueRoles = uniqueRoles;

        return copy;
    }
}

/*
 * ================================================================
 * FUNCIÓN DE EVALUACIÓN ENTREGADA
 * ================================================================
 *
 * No queremos simplemente maximizar BST.
 *
 * Ejemplo problemático:
 * "Mientras más fuerte el Pokémon, mejor".
 *
 * Eso podría favorecer equipos repetidos o muy similares.
 *
 * Buscamos dos propiedades:
 *
 * 1) Poder dentro de un rango deseado.
 * 2) Diversidad entre los integrantes del equipo.
 *
 * FITNESS GENERAL
 * ---------------------------------------------------------------
 * fitness = 0.40 * powerScore + 0.60 * diversityScore
 *
 * DIVERSIDAD
 * ---------------------------------------------------------------
 * diversityScore =
 *     0.25 * uniquePokemonScore
 *   + 0.55 * typeScore
 *   + 0.20 * roleScore
 *
 * EJEMPLO
 * ---------------------------------------------------------------
 * Equipo A:
 * averageBST = 475
 * uniquePokemon = 6
 * uniqueTypes = 9
 * uniqueRoles = 3
 *
 * powerScore = 1.0
 * diversityScore ~= 1.0
 * fitness ~= 1.0
 *
 * Equipo B:
 * [Mewtwo, Mewtwo, Mewtwo, Mewtwo, Mewtwo, Mewtwo]
 *
 * Puede tener estadísticas altas, pero:
 * uniquePokemon = 1
 * uniqueTypes bajo
 * uniqueRoles bajo
 *
 * Por lo tanto, su diversityScore disminuye.
 */
public static class PokemonTeamFitness
{
    private const float POWER_WEIGHT = 0.40f;
    private const float DIVERSITY_WEIGHT = 0.60f;

    private const float UNIQUE_POKEMON_WEIGHT = 0.25f;
    private const float TYPE_WEIGHT = 0.55f;
    private const float ROLE_WEIGHT = 0.20f;

    public static void Evaluate(
        PokemonTeamCandidate candidate,
        IReadOnlyList<PokemonData> dataset,
        PokemonTeamGenerationConfig config)
    {
        if (candidate == null || candidate.genes == null || candidate.genes.Length == 0)
            return;

        float bstSum = 0f;
        HashSet<int> uniquePokemon = new HashSet<int>();
        HashSet<string> uniqueTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<PokemonRole> uniqueRoles = new HashSet<PokemonRole>();

        for (int i = 0; i < candidate.genes.Length; i++)
        {
            int index = candidate.genes[i];
            PokemonData pokemon = dataset[index];

            bstSum += pokemon.bst;
            uniquePokemon.Add(index);

            if (!string.IsNullOrWhiteSpace(pokemon.type1))
                uniqueTypes.Add(pokemon.type1);

            if (!string.IsNullOrWhiteSpace(pokemon.type2))
                uniqueTypes.Add(pokemon.type2);

            uniqueRoles.Add(GetRole(pokemon));
        }

        candidate.averageBST = bstSum / candidate.genes.Length;
        candidate.uniquePokemon = uniquePokemon.Count;
        candidate.uniqueTypes = uniqueTypes.Count;
        candidate.uniqueRoles = uniqueRoles.Count;

        candidate.powerScore = CalculatePowerScore(candidate.averageBST, config);

        float uniquePokemonScore = uniquePokemon.Count / (float)candidate.genes.Length;
        float typeScore = Mathf.Clamp01(
            uniqueTypes.Count / (float)Mathf.Max(1, config.targetUniqueTypes)
        );
        float roleScore = uniqueRoles.Count / 3f;

        candidate.diversityScore =
            UNIQUE_POKEMON_WEIGHT * uniquePokemonScore +
            TYPE_WEIGHT * typeScore +
            ROLE_WEIGHT * roleScore;

        candidate.fitness =
            POWER_WEIGHT * candidate.powerScore +
            DIVERSITY_WEIGHT * candidate.diversityScore;
    }

    public static PokemonRole GetRole(PokemonData pokemon)
    {
        if (pokemon.speed >= pokemon.offense && pokemon.speed >= pokemon.defense)
            return PokemonRole.Fast;

        if (pokemon.defense >= pokemon.offense)
            return PokemonRole.Defensive;

        return PokemonRole.Offensive;
    }

    private static float CalculatePowerScore(
        float averageBST,
        PokemonTeamGenerationConfig config)
    {
        if (averageBST >= config.minAverageBST && averageBST <= config.maxAverageBST)
            return 1f;

        float distance;

        if (averageBST < config.minAverageBST)
            distance = config.minAverageBST - averageBST;
        else
            distance = averageBST - config.maxAverageBST;

        return Mathf.Clamp01(1f - distance / config.bstPenaltyRange);
    }

    /*
     * ============================================================
     * GENERACIÓN DE CONTENIDO
     * ============================================================
     *
     * No necesariamente devolvemos el individuo con el fitness
     * matemáticamente más alto.
     *
     * Ejemplo:
     * Team A -> fitness 0.96
     * Team B -> fitness 0.95
     * Team C -> fitness 0.94
     * Team D -> fitness 0.78
     *
     * threshold = 0.90
     *
     * candidatos válidos = [Team A, Team B, Team C]
     *
     * Se selecciona aleatoriamente uno de ellos.
     * Esto permite generar diferentes contenidos válidos.
     */
    public static PokemonTeamCandidate SelectGeneratedContent(
        List<PokemonTeamCandidate> population,
        PokemonTeamGenerationConfig config,
        System.Random random)
    {
        if (population == null || population.Count == 0)
            return null;

        List<PokemonTeamCandidate> ordered = new List<PokemonTeamCandidate>(population);
        ordered.Sort((a, b) => b.fitness.CompareTo(a.fitness));

        List<PokemonTeamCandidate> valid = new List<PokemonTeamCandidate>();

        foreach (PokemonTeamCandidate candidate in ordered)
        {
            if (candidate.fitness >= config.outputFitnessThreshold)
                valid.Add(candidate);
        }

        if (valid.Count > 0)
            return valid[random.Next(valid.Count)].Clone();

        int topCount = Mathf.Min(
            Mathf.Max(1, config.fallbackTopCandidates),
            ordered.Count
        );

        return ordered[random.Next(topCount)].Clone();
    }
}

[Serializable]
public class PokemonTeamSlotUI
{
    public TMP_Text nameText;
    public TMP_Text typesText;
    public TMP_Text statsText;
}

public class PokemonTeamController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PokemonCSVLoader loader;

    [Header("Algorithms")]
    [SerializeField] private GeneticAlgorithm geneticAlgorithm;
    [SerializeField] private EvolutionaryStrategy evolutionaryStrategy;

    [SerializeField]
    private TeamGenerationMethod generationMethod = TeamGenerationMethod.GeneticAlgorithm;

    [Header("Content Configuration")]
    [SerializeField]
    private PokemonTeamGenerationConfig config = new PokemonTeamGenerationConfig();

    [Header("Team UI")]
    [SerializeField]
    private PokemonTeamSlotUI[] teamSlots = new PokemonTeamSlotUI[6];

    [Header("Summary UI")]
    [SerializeField] private TMP_Text summaryText;

    public void GenerateTeam()
    {
        if (loader == null)
        {
            Debug.LogError("[PokemonTeamController] Loader not assigned.");
            return;
        }

        if (!loader.IsLoaded)
            loader.Load();

        if (loader.Data.Count == 0)
        {
            Debug.LogError("[PokemonTeamController] Dataset is empty.");
            return;
        }

        PokemonTeamCandidate result = null;

        switch (generationMethod)
        {
            case TeamGenerationMethod.GeneticAlgorithm:
                if (geneticAlgorithm == null)
                    return;

                result = geneticAlgorithm.Generate(loader.Data, config);
                break;

            case TeamGenerationMethod.EvolutionaryStrategy:
                if (evolutionaryStrategy == null)
                    return;

                result = evolutionaryStrategy.Generate(loader.Data, config);
                break;
        }

        if (result == null)
        {
            Debug.LogError("[PokemonTeamController] No team was generated.");
            return;
        }

        DisplayTeam(result);
    }

    public void SetGenerationMethod(int value)
    {
        generationMethod = value == 0
            ? TeamGenerationMethod.GeneticAlgorithm
            : TeamGenerationMethod.EvolutionaryStrategy;
    }

    private void DisplayTeam(PokemonTeamCandidate team)
    {
        int count = Mathf.Min(team.genes.Length, teamSlots.Length);

        for (int i = 0; i < count; i++)
        {
            PokemonData pokemon = loader.Data[team.genes[i]];
            PokemonTeamSlotUI slot = teamSlots[i];

            if (slot.nameText != null)
                slot.nameText.text = pokemon.name;

            if (slot.typesText != null)
            {
                slot.typesText.text = string.IsNullOrWhiteSpace(pokemon.type2)
                    ? pokemon.type1
                    : $"{pokemon.type1} / {pokemon.type2}";
            }

            if (slot.statsText != null)
            {
                PokemonRole role = PokemonTeamFitness.GetRole(pokemon);

                slot.statsText.text =
                    $"HP {pokemon.hp}   OFF {pokemon.offense}\n" +
                    $"DEF {pokemon.defense}   SPD {pokemon.speed}\n" +
                    $"BST {pokemon.bst}   {role}";
            }
        }

        if (summaryText != null)
        {
            summaryText.text =
                $"Method: {generationMethod}\n" +
                $"Fitness: {team.fitness:0.000}\n" +
                $"Power: {team.powerScore:0.000}   Diversity: {team.diversityScore:0.000}\n" +
                $"Average BST: {team.averageBST:0.0}\n" +
                $"Unique Pokemon: {team.uniquePokemon}/{team.genes.Length}\n" +
                $"Unique Types: {team.uniqueTypes}\n" +
                $"Roles: {team.uniqueRoles}/3";
        }
    }
}
