using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Shiningforce
{
    public class UI : MonoBehaviour
    {
        public static UI Instance = null;

        public TextMeshProUGUI Debug1 = null;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
        }

        void Update()
        {
        }
    }
}
