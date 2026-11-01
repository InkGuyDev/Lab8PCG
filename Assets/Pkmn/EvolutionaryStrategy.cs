using System;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionaryStrategy : MonoBehaviour
{
    [Header("(mu + lambda) Evolution Strategy")]

    /*
     * mu = cantidad de padres que sobreviven.
     *
     * mu = 15
     * parents.Count = 15
     *
     * parents = [
     *   [25, 131, 94, 212, 135, 445],
     *   [6, 150, 197, 376, 59, 248],
     *   ...
     *   [...] // padre 14
     * ]
     */
    [Min(1)]
    [SerializeField] private int mu = 15;

    /*
     * lambda = cantidad de hijos generados en cada generación.
     *
     * lambda = 45
     * offspring.Count = 45
     *
     * Cada hijo se obtiene copiando un padre y aplicando mutación.
     */
    [Min(1)]
    [SerializeField] private int lambda = 45;

    [Min(1)]
    [SerializeField] private int generations = 80;

    /*
     * mutationRate = probabilidad de mutar cada gen.
     *
     * mutationRate = 0.20
     *
     * Cada una de las 6 posiciones tiene 20% de probabilidad
     * de ser reemplazada por otro Pokémon.
     */
    [Range(0f, 1f)]
    [SerializeField] private float mutationRate = 0.20f;

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;

    /*
     * ============================================================
     * (mu + lambda) EVOLUTION STRATEGY
     * ============================================================
     *
     * DIFERENCIA PRINCIPAL CON GA
     * ------------------------------------------------------------
     * - NO utilizamos crossover.
     * - Los hijos se crean desde un padre.
     * - La principal fuente de variación es la mutación.
     * - Padres e hijos compiten juntos.
     *
     * PSEUDOCÓDIGO GENERAL
     * ------------------------------------------------------------
     * 1: create mu random parents
     * 2: evaluate parents
     *
     * 3: repeat for N generations:
     * 4:      create empty offspring
     *
     * 5:      repeat lambda times:
     * 6:          choose random parent
     * 7:          child = copy(parent)
     * 8:          mutate(child)
     * 9:          evaluate(child)
     * 10:         add child to offspring
     *
     * 11:     combined = parents + offspring
     * 12:     sort combined by fitness
     * 13:     keep best mu individuals as new parents
     *
     * 14: select generated content from final parents
     *
     * EJEMPLO
     * ------------------------------------------------------------
     * mu = 2
     * lambda = 3
     *
     * parents:
     * A -> fitness 0.80
     * B -> fitness 0.72
     *
     * offspring:
     * C -> fitness 0.88
     * D -> fitness 0.65
     * E -> fitness 0.91
     *
     * combined = [A, B, C, D, E]
     *
     * ordenado:
     * E = 0.91
     * C = 0.88
     * A = 0.80
     * B = 0.72
     * D = 0.65
     *
     * Como mu = 2:
     * newParents = [E, C]
     */
    public PokemonTeamCandidate Generate(
        IReadOnlyList<PokemonData> dataset,
        PokemonTeamGenerationConfig config)
    {
        if (dataset == null || dataset.Count == 0)
            return null;

        System.Random random = new System.Random();

        // TODO 1: Crear "mu" padres aleatorios.
        // TODO 2: Evaluar padres.
        // TODO 3: Repetir durante "generations".
        //
        // En cada generación:
        // - crear lambda hijos
        // - elegir un padre aleatorio para cada hijo
        // - copiarlo
        // - mutarlo
        // - evaluarlo
        //
        // Después:
        // combined = parents + offspring
        // ordenar combined por fitness
        // conservar solamente los mejores "mu"

        List<PokemonTeamCandidate> parents = CreateInitialParents(mu, dataset.Count, config, random);

        EvaluatePopulation(parents, dataset, config);

        for (int gen = 0; gen < generations; gen++)
        {
            List<PokemonTeamCandidate> offspring = new List<PokemonTeamCandidate>();

            for (int i = 0; i < lambda; i++)
            {
                PokemonTeamCandidate selectdParent = parents[random.Next(0, parents.Count)];
                PokemonTeamCandidate child = selectdParent.Clone();

                Mutate(child, dataset.Count, random);
                PokemonTeamFitness.Evaluate(child, dataset, config);

                offspring.Add(child);
            }

            List<PokemonTeamCandidate> PokemonCombined = new List<PokemonTeamCandidate>();
            PokemonCombined.AddRange(parents);
            PokemonCombined.AddRange(offspring);

            PokemonCombined.Sort((a, b) => b.fitness.CompareTo(a.fitness));

            parents = PokemonCombined.GetRange(0, mu);

            if (logProgress)
            {
                Debug.Log($"[EvolutionaryStrategy] Gen {gen + 1}/{generations} - Best Fitness: {parents[0].fitness:F4}");
            }
        }

        //Debug.LogWarning(
        //    "[EvolutionaryStrategy] TODO: implementar Generate()."
        //);

        return parents[0];
    }

    /*
     * ============================================================
     * CREAR PADRES INICIALES
     * ============================================================
     *
     * mu = 15
     * teamSize = 6
     *
     * parents.Count = 15
     *
     * Cada padre puede verse así:
     * [25, 131, 94, 212, 135, 445]
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: create empty parents
     * 2: repeat parentCount times:
     * 3:      create candidate with teamSize genes
     * 4:      assign random dataset index to every gene
     * 5:      add candidate to parents
     * 6: return parents
     */
    private List<PokemonTeamCandidate> CreateInitialParents(
        int parentCount,
        int datasetSize,
        PokemonTeamGenerationConfig config,
        System.Random random)
    {
        List<PokemonTeamCandidate> parents = new List<PokemonTeamCandidate>();

        for (int i = 0; i < parentCount; i++)
        {
            PokemonTeamCandidate candidate = new PokemonTeamCandidate(config.teamSize);

            for (int j = 0; j < config.teamSize; j++)
            {
                candidate.genes[j] = random.Next(0, datasetSize);
            }

            parents.Add(candidate);
        }

        return parents;
    }

    /*
     * ============================================================
     * MUTACIÓN
     * ============================================================
     *
     * Padre:
     * [25, 131, 94, 212, 135, 445]
     *
     * Copia:
     * [25, 131, 94, 212, 135, 445]
     *
     * Después de mutar:
     * [25, 131, 700, 212, 135, 445]
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: mutated = false
     * 2: for each gene:
     * 3:      if random < mutationRate:
     * 4:          replace gene with another random Pokemon
     * 5:          mutated = true
     *
     * 6: if no gene was mutated:
     * 7:      choose one random gene
     * 8:      replace it with another Pokemon
     *
     * Queremos asegurar que cada hijo tenga al menos una variación.
     */
    private void Mutate(
        PokemonTeamCandidate candidate,
        int datasetSize,
        System.Random random)
    {
        bool mutated = false;
        int index = 0;

        foreach (int pokemon in candidate.genes)
        {
            if (random.NextDouble() < mutationRate)
            {
                candidate.genes[index] = RandomDifferentPokemon(pokemon, datasetSize, random);
                mutated = true;
            }
            index++;
        }

        if (!mutated)
        {
            int randomIndex = random.Next(0, candidate.genes.Length);
            int currentPokemon = candidate.genes[randomIndex];
            candidate.genes[randomIndex] = RandomDifferentPokemon(currentPokemon, datasetSize, random);
        }
    }


    /*
     * Retorna un índice diferente al actual.
     *
     * current = 94
     * datasetSize = 1032
     *
     * resultado posible = 700
     *
     * Debe estar entre 0 y datasetSize - 1
     * y no debe ser igual a current.
     */
    private int RandomDifferentPokemon(
        int current,
        int datasetSize,
        System.Random random)
    {
        if (datasetSize <= 1)
        {
            return current;
        }

        int newIndex;

        do
        {
            newIndex = random.Next(0, datasetSize);
        }
        while (newIndex == current);

        return newIndex;
    }

    private void EvaluatePopulation(
        List<PokemonTeamCandidate> population,
        IReadOnlyList<PokemonData> dataset,
        PokemonTeamGenerationConfig config)
    {
        foreach (PokemonTeamCandidate candidate in population)
        {
            PokemonTeamFitness.Evaluate(candidate, dataset, config);
        }
    }
}
