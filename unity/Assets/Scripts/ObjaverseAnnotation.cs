using System;
using UnityEngine;

namespace Thor.Objaverse {

    public enum Dataset {
        Objaverse1_0,
        ObjaversePlus,
        ObjaverseXL
    }

    public class ObjaverseAnnotation : MonoBehaviour
    {
        [SerializeField] public string ObjectCategory;

        [SerializeField] public Dataset MostSpecificDataset = Dataset.Objaverse1_0;
    }


}