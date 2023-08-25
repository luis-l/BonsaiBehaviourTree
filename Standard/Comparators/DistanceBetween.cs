using System;
using Bonsai;
using Bonsai.Core;
using UnityEngine;

namespace Bonsai.Standard
{
    /// <summary>
    /// Compares the distance between two Vector3 points.
    /// </summary>
    [BonsaiNode("Comparators/", "DistanceBetween")]
    public class DistanceBetween : Comparator<Vector3>
    {
        private enum Type
        {
            Equal,
            Less,
            Greater
        };

        [SerializeField] private Type _type = Type.Less;
        [SerializeField] private float _value = 2;

        protected override bool Compare(Vector3 x, Vector3 y)
        {
            var distance = Vector3.Distance(x, y);
            var isSuccess = _type switch
            {
                Type.Equal => Math.Abs(distance - _value) < 0.01f,
                Type.Less => distance < _value,
                Type.Greater => distance > _value,
                _ => throw new ArgumentOutOfRangeException()
            };

            return isSuccess;
        }
    }
}