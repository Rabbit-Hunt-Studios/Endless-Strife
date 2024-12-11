using System;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using UnityEditor;
using UnityEngine;

namespace TbsFramework.EditorUtils.GridGenerators
{
    public abstract class ICellGridGenerator
    {
        [HideInInspector]
        public Transform CellsParent;
        [HideInInspector]
        public bool Is2D;

        Dictionary<string, object> generatorParameterValues = new Dictionary<string, object>();

        public abstract GridInfo GenerateGrid();
        protected GridInfo GetGridInfo(List<Cell> cells)
        {

            var minX = cells.Find(c => c.transform.position.x.Equals(cells.Min(c2 => c2.transform.position.x))).transform.position.x;
            var maxX = cells.Find(c => c.transform.position.x.Equals(cells.Max(c2 => c2.transform.position.x))).transform.position.x;

            var minY = float.MinValue;
            var maxY = float.MaxValue;

            if (Is2D)
            {
                minY = cells.Find(c => c.transform.position.y.Equals(cells.Min(c2 => c2.transform.position.y))).transform.position.y;
                maxY = cells.Find(c => c.transform.position.y.Equals(cells.Max(c2 => c2.transform.position.y))).transform.position.y;
            }
            else
            {
                minY = cells.Find(c => c.transform.position.z.Equals(cells.Min(c2 => c2.transform.position.z))).transform.position.z;
                maxY = cells.Find(c => c.transform.position.z.Equals(cells.Max(c2 => c2.transform.position.z))).transform.position.z;
            }

            GridInfo gridInfo = new GridInfo();
            gridInfo.Cells = cells;

            gridInfo.Dimensions = Is2D ? new Vector3(maxX - minX, maxY - minY, 0) : new Vector3(maxX - minX, 0, maxY - minY);
            gridInfo.Center = gridInfo.Dimensions / 2 + (Is2D ? new Vector3(minX, minY, 0) : new Vector3(minX, 0, minY));

            return gridInfo;
        }

        public virtual Dictionary<string, object> ReadGeneratorParams()
        {
            Dictionary<Type, Func<string, object, object>> parameterHandlers = new Dictionary<Type, Func<string, object, object>>();
            parameterHandlers.Add(typeof(int), (string x, object y) => EditorGUILayout.IntField(new GUIContent(x), (int)y));
            parameterHandlers.Add(typeof(double), (string x, object y) => EditorGUILayout.DoubleField(new GUIContent(x), (double)y));
            parameterHandlers.Add(typeof(float), (string x, object y) => EditorGUILayout.FloatField(new GUIContent(x), (float)y));
            parameterHandlers.Add(typeof(string), (string x, object y) => EditorGUILayout.TextField(new GUIContent(x), (string)y));
            parameterHandlers.Add(typeof(bool), (string x, object y) => EditorGUILayout.Toggle(new GUIContent(x), (bool)y));
            parameterHandlers.Add(typeof(GameObject), (string x, object y) => EditorGUILayout.ObjectField(x, (GameObject)y, typeof(GameObject), false, new GUILayoutOption[0]));

            foreach (var field in GetType().GetFields().Where(f => f.IsPublic))
            {
                if (!parameterHandlers.ContainsKey(field.FieldType) || Attribute.GetCustomAttribute(field, typeof(HideInInspector)) != null)
                {
                    continue;
                }

                var parameterHandler = parameterHandlers[field.FieldType];
                object value = field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null;

                if (generatorParameterValues.ContainsKey(field.Name))
                {
                    value = generatorParameterValues[field.Name];
                }
                value = parameterHandler(field.Name, value);
                generatorParameterValues[field.Name] = value;
            }

            return generatorParameterValues;
        }
    }

    public class GridInfo
    {
        public Vector3 Dimensions { get; set; }
        public Vector3 Center { get; set; }
        public List<Cell> Cells { get; set; }
    }
}



