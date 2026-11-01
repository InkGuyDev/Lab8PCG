using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratorios.Lab7.WaveFunctionCollapse
{
    public class WFCZoomController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 2f;  // Cuánto puede acercarse máximo
        [SerializeField] private float maxZoom = 25f; // Cuánto puede alejarse máximo

        private void Start()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            // Detectar el movimiento de la rueda del mouse
            float scrollData = Input.GetAxis("Mouse ScrollWheel");

            if (scrollData != 0f && targetCamera != null && targetCamera.orthographic)
            {
                // Modificar el tamaño ortográfico (invertimos el signo para que el zoom sea natural)
                targetCamera.orthographicSize -= scrollData * zoomSpeed;

                // Limitar el zoom para que la cámara no se acerque o se aleje demasiado
                targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize, minZoom, maxZoom);
            }
        }
    }
}
