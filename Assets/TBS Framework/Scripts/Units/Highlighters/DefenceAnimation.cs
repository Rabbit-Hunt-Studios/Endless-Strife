using System.Collections;
using UnityEngine;

namespace TbsFramework.Units.Highlighters
{
    public class DefenceAnimation : UnitHighlighter
    {
        [SerializeField] private float _magnitude = 1;

        public override void Apply(Unit unit, Unit otherUnit)
        {
            StartCoroutine(DefenceAnimationCoroutine(unit));
        }

        private IEnumerator DefenceAnimationCoroutine(Unit unit)
        {
            var StartingPosition = unit.transform.position;
            var rnd = new System.Random();

            for (int i = 0; i < 5; i++)
            {
                var heading = new Vector3(((float)rnd.NextDouble() - 0.5f), (float)rnd.NextDouble() - 0.5f, 0);
                var direction = (heading / heading.magnitude) * _magnitude;
                float startTime = Time.time;

                while (startTime + 0.05f > Time.time)
                {
                    unit.transform.position = Vector3.Lerp(transform.position, transform.position + direction, ((startTime + 0.05f) - Time.time));
                    yield return null;
                }
                startTime = Time.time;
                while (startTime + 0.05f > Time.time)
                {
                    unit.transform.position = Vector3.Lerp(transform.position, transform.position - direction, ((startTime + 0.05f) - Time.time));
                    yield return null;
                }
            }
            unit.transform.position = StartingPosition;
        }
    }
}
