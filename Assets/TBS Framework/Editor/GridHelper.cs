using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;
using TbsFramework.Grid;
using TbsFramework.Cells;
using TbsFramework.Gui;
using TbsFramework.Players;
using TbsFramework.Units;
using TbsFramework.Grid.UnitGenerators;
using TbsFramework.EditorUtils.GridGenerators;
using TbsFramework.Grid.TurnResolvers;
using TbsFramework.Grid.GameResolvers;
using UnityEngine.EventSystems;

namespace TbsFramework.EditorUtils
{
    class GridHelper : EditorWindow
    {
        private static string MAP_TYPE_3D = "XZ Plane (3D)";
        private static string MAP_TYPE_2D = "XY Plane (2D)";

        public bool keepMainCamera = false;
        public bool useMovableCamera = false;
        public float cameraScrollSpeed = 15f;
        public float cameraScrollEdge = 0.01f;

        public int nHumanPlayer = 2;
        public int nComputerPlayer;

        public int generatorIndex;
        public int mapTypeIndex;

        public static List<Type> generators;
        public static string[] generatorNames;
        public static string[] mapTypes;

        GameObject cellGrid;
        GameObject units;
        GameObject players;
        GameObject guiController;
        GameObject directionalLight;

        ICellGridGenerator generator;
        Dictionary<string, object> parameterValues;

        BoolWrapper tileEditModeOn = new BoolWrapper(false);
        [SerializeField]
        Cell tilePrefab;
        int tilePaintingRadius = 1;
        int lastPaintedHash = -1;

        BoolWrapper unitEditModeOn = new BoolWrapper(false);
        [SerializeField]
        Unit unitPrefab;
        int playerNumber;

        bool gridGameObjectPresent;
        bool unitsGameObjectPresent;
        GameObject gridGameObject;
        GameObject unitsGameObject;

        BoolWrapper toToggle = null;

        bool shouldDisplayCollider2DWarning;
        bool shouldDisplayNoColliderOnCellWarning;
        private Vector2 scrollPosition = Vector2.zero;

        [MenuItem("Window/Grid Helper")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(GridHelper));
            window.titleContent.text = "Grid Helper";
        }

        void Initialize()
        {
            if (parameterValues == null)
            {
                parameterValues = new Dictionary<string, object>();
            }

            if (generators == null)
            {
                Type generatorInterface = typeof(ICellGridGenerator);
                var assembly = generatorInterface.Assembly;

                generators = new List<Type>();
                foreach (var t in assembly.GetTypes())
                {
                    if (generatorInterface.IsAssignableFrom(t) && t != generatorInterface)
                        generators.Add(t);
                }
            }

            if (generatorNames == null)
            {
                generatorNames = generators.Select(t => t.Name).ToArray();
            }

            if (mapTypes == null)
            {
                mapTypes = new string[2];
                mapTypes[0] = MAP_TYPE_2D;
                mapTypes[1] = MAP_TYPE_3D;
            }
        }

        public void OnEnable()
        {
            Initialize();

            var gridGameObject = GameObject.Find("CellGrid");
            var unitsGameObject = GameObject.Find("Units");

            gridGameObjectPresent = gridGameObject != null;
            unitsGameObjectPresent = unitsGameObject != null;

            tileEditModeOn = new BoolWrapper(false);
            unitEditModeOn = new BoolWrapper(false);

            EnableSceneViewInteraction();
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += OnUndoPerformed;
        }


        public void OnDestroy()
        {
            DisableSceneViewInteraction();
            tileEditModeOn = new BoolWrapper(false);
            unitEditModeOn = new BoolWrapper(false);

            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= OnUndoPerformed;
        }

        void OnHierarchyChange()
        {
            var gridGameObject = GameObject.Find("CellGrid");
            var unitsGameObject = GameObject.Find("Units");

            gridGameObjectPresent = gridGameObject != null;
            unitsGameObjectPresent = unitsGameObject != null;

            if (unitsGameObject != null)
            {
                this.unitsGameObject = null;
            }
            if (gridGameObject != null)
            {
                this.gridGameObject = null;
            }

            Repaint();
        }

        void OnGUI()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUIStyle.none);
            MapGenerationGUI();
            TilePaintingGUI();
            UnitPaintingGUI();
            PrefabHelperGUI();

            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.R)
            {
                ToggleEditMode();
            }
            GUILayout.EndScrollView();
        }

        private void ToggleEditMode()
        {
            if (toToggle == null)
            {
                return;
            }
            toToggle.value = !toToggle.value;
            if (toToggle.value)
            {
                EnableSceneViewInteraction();
            }
            Repaint();
        }

        private void PrefabHelperGUI()
        {
            GUILayout.Label("Prefab helper", EditorStyles.boldLabel);
            GUILayout.Label("Select multiple objects in hierarchy and click button below to create multiple prefabs at once. Please note that this may take a while", EditorStyles.wordWrappedLabel);

            if (GUILayout.Button("Selection to prefabs"))
            {
                string path = EditorUtility.SaveFolderPanel("Save prefabs", "", "");
                if (path.Length != 0)
                {
                    path = path.Replace(Application.dataPath, "Assets");

                    GameObject[] objectArray = Selection.gameObjects;
                    for (int i = 0; i < objectArray.Length; i++)
                    {
                        GameObject gameObject = objectArray[i];
                        string localPath = path + "/" + gameObject.name + ".prefab";
                        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
                        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, localPath, InteractionMode.UserAction);
                    }
                    Debug.Log(string.Format("{0} prefabs saved to {1}", objectArray.Length, path));
                }
            }

            if (GUILayout.Button("Selection to prefabs (variants)"))
            {
                string path = EditorUtility.SaveFolderPanel("Save prefabs", "", "");
                if (path.Length != 0)
                {
                    path = path.Replace(Application.dataPath, "Assets");

                    Transform[] objectArray = Selection.transforms;
                    GameObject root = objectArray[0].gameObject;

                    string localPath = path + "/" + root.name + ".prefab";
                    localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

                    var rootPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, localPath, InteractionMode.UserAction);

                    for (int i = 1; i < objectArray.Length; i++)
                    {
                        GameObject gameObject = objectArray[i].gameObject;
                        var rootInstance = PrefabUtility.InstantiatePrefab(rootPrefab) as GameObject;

                        foreach (var component in gameObject.GetComponents<Component>())
                        {
                            var destComponent = rootInstance.GetComponent(component.GetType());
                            if (destComponent)
                            {
                                EditorUtility.CopySerialized(component, rootInstance.GetComponent(component.GetType()));
                            }
                        }
                        localPath = path + "/" + gameObject.name + ".prefab";
                        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
                        PrefabUtility.SaveAsPrefabAssetAndConnect(rootInstance, localPath, InteractionMode.UserAction);

                        DestroyImmediate(rootInstance);
                    }
                    Debug.Log(string.Format("{0} prefabs saved to {1}", objectArray.Length, path));
                }
            }
        }

        private void UnitPaintingGUI()
        {
            GUILayout.Label("Unit painting", EditorStyles.boldLabel);
            if (!unitsGameObjectPresent)
            {
                if (unitsGameObject == null)
                {
                    GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                    style.normal.textColor = Color.red;
                    GUILayout.Label("Unit parent GameObject missing", style);
                }
                unitsGameObject = (GameObject)EditorGUILayout.ObjectField("Units parent", unitsGameObject, typeof(GameObject), true, new GUILayoutOption[0]);
            }
            unitPrefab = (Unit)EditorGUILayout.ObjectField("Unit prefab", unitPrefab, typeof(Unit), false, new GUILayoutOption[0]);

            if (unitPrefab != null
                && unitPrefab.GetComponent<Collider>() == null
                && unitPrefab.GetComponentInChildren<Collider>() == null
                && unitPrefab.GetComponent<Collider2D>() == null
                && unitPrefab.GetComponentInChildren<Collider2D>() == null)
            {
                GUIStyle style = new GUIStyle(EditorStyles.wordWrappedLabel);
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.red;
                GUILayout.Label("Please add a collider to your unit prefab. Without the collider the scene will not be playable", style);
            }

            playerNumber = EditorGUILayout.IntField(new GUIContent("Player number"), playerNumber);
            GUILayout.Label(string.Format("Unit Edit Mode is {0}", unitEditModeOn.value ? "on" : "off"));

            if (toToggle != null && toToggle == unitEditModeOn)
            {
                GUILayout.Label("Press Ctrl + R to toggle unit painting mode on / off");
            }

            if (unitEditModeOn.value)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Enter Unit Edit Mode"))
            {
                unitEditModeOn = new BoolWrapper(true);
                tileEditModeOn = new BoolWrapper(false);
                toToggle = unitEditModeOn;

                GameObject UnitsParent = unitsGameObjectPresent ? GameObject.Find("Units") : unitsGameObject;
                if (UnitsParent == null)
                {
                    Debug.LogError("Units parent gameobject is missing, assign it in GridHelper");
                }
            }
            GUI.enabled = true;
            if (!unitEditModeOn.value)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Exit Unit Edit Mode"))
            {
                unitEditModeOn = new BoolWrapper(false);
                DisableSceneViewInteraction();
            }
            GUI.enabled = true;
        }

        private void TilePaintingGUI()
        {
            GUILayout.Label("Tile painting", EditorStyles.boldLabel);
            if (!gridGameObjectPresent)
            {
                if (gridGameObject == null)
                {
                    GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                    style.normal.textColor = Color.red;
                    GUILayout.Label("CellGrid GameObject missing", style);
                }
                gridGameObject = (GameObject)EditorGUILayout.ObjectField("CellGrid", gridGameObject, typeof(GameObject), true, new GUILayoutOption[0]);
            }
            tilePaintingRadius = EditorGUILayout.IntSlider(new GUIContent("Brush radius"), tilePaintingRadius, 1, 4);
            EditorGUI.BeginChangeCheck();
            tilePrefab = (Cell)EditorGUILayout.ObjectField("Tile prefab", tilePrefab, typeof(Cell), true, new GUILayoutOption[0]);

            if (tilePrefab != null
                && tilePrefab.GetComponent<Collider>() == null
                && tilePrefab.GetComponentInChildren<Collider>() == null
                && tilePrefab.GetComponent<Collider2D>() == null
                && tilePrefab.GetComponentInChildren<Collider2D>() == null)
            {
                GUIStyle style = new GUIStyle(EditorStyles.wordWrappedLabel);
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.red;
                GUILayout.Label("Please add a collider to your cell prefab. Without the collider the scene will not be playable", style);
            }

            if (EditorGUI.EndChangeCheck())
            {
                lastPaintedHash = -1;
            }
            GUILayout.Label(string.Format("Tile Edit Mode is {0}", tileEditModeOn.value ? "on" : "off"));

            if (toToggle != null && toToggle == tileEditModeOn)
            {
                GUILayout.Label("Press Ctrl + R to toggle tile painting mode on / off");
            }

            if (tileEditModeOn.value)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Enter Tile Edit Mode"))
            {
                tileEditModeOn = new BoolWrapper(true);
                unitEditModeOn = new BoolWrapper(false);
                toToggle = tileEditModeOn;
                EnableSceneViewInteraction();

                GameObject CellGrid = gridGameObjectPresent ? GameObject.Find("CellGrid") : gridGameObject;
                if (CellGrid == null)
                {
                    Debug.LogError("CellGrid gameobject is missing, assign it in GridHelper");
                }
            }
            GUI.enabled = true;
            if (!tileEditModeOn.value)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Exit Tile Edit Mode"))
            {
                tileEditModeOn = new BoolWrapper(false);
                DisableSceneViewInteraction();
            }
            GUI.enabled = true;
        }

        private void MapGenerationGUI()
        {
            GUILayout.Label("Grid generation", EditorStyles.boldLabel);
            GUILayout.Label("Camera", EditorStyles.boldLabel);
            useMovableCamera = EditorGUILayout.Toggle(new GUIContent("Use movable camera", "Determines whether to add a movement controller to the Main Camera"), useMovableCamera, new GUILayoutOption[0]);

            if (useMovableCamera)
            {
                cameraScrollSpeed = EditorGUILayout.FloatField(new GUIContent("Scroll Speed"), cameraScrollSpeed);
                cameraScrollEdge = EditorGUILayout.Slider("Scroll Edge", cameraScrollEdge, 0.05f, 0.25f, new GUILayoutOption[0]);
            }

            GUILayout.Label("Players", EditorStyles.boldLabel);
            nHumanPlayer = EditorGUILayout.IntField(new GUIContent("Human players No"), nHumanPlayer);
            nComputerPlayer = EditorGUILayout.IntField(new GUIContent("AI players No"), nComputerPlayer);

            GUILayout.Label("Grid", EditorStyles.boldLabel);

            mapTypeIndex = EditorGUILayout.Popup("Plane", mapTypeIndex, mapTypes, new GUILayoutOption[0]);

            EditorGUI.BeginChangeCheck();
            generatorIndex = EditorGUILayout.Popup("Generator", generatorIndex, generatorNames, new GUILayoutOption[0]);
            if (EditorGUI.EndChangeCheck() || generator == null)
            {
                parameterValues = new Dictionary<string, object>();
                Type selectedGenerator = generators[generatorIndex];
                generator = (ICellGridGenerator)Activator.CreateInstance(selectedGenerator);
            }

            parameterValues = generator.ReadGeneratorParams();
            keepMainCamera = EditorGUILayout.Toggle(new GUIContent("Keep main camera", "Determines whether to keep the current Main Camera or create a new one"), keepMainCamera, new GUILayoutOption[0]);

            if (GUILayout.Button("Generate scene"))
            {
                Undo.ClearAll();
                GenerateBaseStructure();
            }
            if (GUILayout.Button("Clear scene"))
            {
                string dialogTitle = "Confirm delete";
                string dialogMessage = "This will delete all objects on the scene. Do you wish to continue?";
                string dialogOK = "Ok";
                string dialogCancel = "Cancel";

                bool shouldDelete = EditorUtility.DisplayDialog(dialogTitle, dialogMessage, dialogOK, dialogCancel);
                if (shouldDelete)
                {
                    Undo.ClearAll();
                    GridHelperUtils.ClearScene(false);
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.R)
            {
                ToggleEditMode();
            }

            if (tileEditModeOn.value || unitEditModeOn.value)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }

            if (tileEditModeOn.value && tilePrefab != null)
            {
                PaintTiles();
            }
            if (unitEditModeOn.value && unitPrefab != null)
            {
                PaintUnits();
            }
        }

        private void PaintUnits()
        {
            GameObject UnitsParent = unitsGameObjectPresent ? GameObject.Find("Units") : unitsGameObject;
            if (UnitsParent == null)
            {
                return;
            }

            var selectedCell = GetSelectedCell();
            if (selectedCell == null)
            {
                return;
            }

            var mapType = mapTypes[mapTypeIndex];
            Handles.color = Color.red;
            Handles.DrawWireDisc(selectedCell.transform.position, Vector3.up, (mapType.Equals(MAP_TYPE_2D) ? selectedCell.GetCellDimensions().y : selectedCell.GetCellDimensions().z) / 2);
            Handles.DrawWireDisc(selectedCell.transform.position, Vector3.forward, (mapType.Equals(MAP_TYPE_2D) ? selectedCell.GetCellDimensions().y : selectedCell.GetCellDimensions().z) / 2);
            HandleUtility.Repaint();
            if (Event.current.button == 0 && (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown))
            {
                if (unitEditModeOn.value && selectedCell.IsTaken)
                {
                    return;
                }

                Undo.SetCurrentGroupName("Unit painting");
                int group = Undo.GetCurrentGroup();

                Undo.RecordObject(selectedCell, "Unit painting");
                var newUnit = (PrefabUtility.InstantiatePrefab(unitPrefab.gameObject) as GameObject).GetComponent<Unit>();
                newUnit.PlayerNumber = playerNumber;
                newUnit.Cell = selectedCell;

                selectedCell.IsTaken = newUnit.Obstructable;

                newUnit.transform.position = selectedCell.transform.position;
                newUnit.transform.parent = UnitsParent.transform;
                newUnit.transform.rotation = selectedCell.transform.rotation;

                Undo.RegisterCreatedObjectUndo(newUnit.gameObject, "Unit painting");
            }
        }

        private void PaintTiles()
        {
            GameObject CellGrid = gridGameObjectPresent ? GameObject.Find("CellGrid") : gridGameObject;
            if (CellGrid == null)
            {
                return;
            }

            Cell selectedCell = GetSelectedCell();
            if (selectedCell == null)
            {
                return;
            }

            var mapType = mapTypes[mapTypeIndex];
            Handles.color = Color.red;
            Handles.DrawWireDisc(selectedCell.transform.position, Vector3.up, (mapType.Equals(MAP_TYPE_2D) ? selectedCell.GetCellDimensions().y : selectedCell.GetCellDimensions().z) * (tilePaintingRadius - 0.5f));
            Handles.DrawWireDisc(selectedCell.transform.position, Vector3.forward, (mapType.Equals(MAP_TYPE_2D) ? selectedCell.GetCellDimensions().y : selectedCell.GetCellDimensions().z) * (tilePaintingRadius - 0.5f));
            HandleUtility.Repaint();
            int selectedCellHash = selectedCell.GetHashCode();
            if (lastPaintedHash != selectedCellHash)
            {
                if (Event.current.button == 0 && (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown))
                {
                    lastPaintedHash = selectedCellHash;
                    Undo.SetCurrentGroupName("Tile painting");
                    int group = Undo.GetCurrentGroup();
                    var cells = CellGrid.GetComponentsInChildren<Cell>();
                    var cellsInRange = cells.Where(c => c.GetDistance(selectedCell) <= tilePaintingRadius - 1).ToList();
                    foreach (var c in cellsInRange)
                    {
                        if (tilePrefab == PrefabUtility.GetCorrespondingObjectFromSource(c))
                        {
                            continue;
                        }
                        var newCell = (PrefabUtility.InstantiatePrefab(tilePrefab.gameObject, c.transform.parent) as GameObject).GetComponent<Cell>();
                        newCell.transform.position = c.transform.position;

                        try
                        {
                            c.CopyFields(newCell);
                            Undo.RegisterCreatedObjectUndo(newCell.gameObject, "Tile painting");
                            Undo.DestroyObjectImmediate(c.gameObject);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(string.Format("{0} - Probably you are using wrong tile prefab", e.Message));
                            DestroyImmediate(newCell.gameObject);
                        }

                    }
                    Undo.CollapseUndoOperations(group);
                    Undo.IncrementCurrentGroup();
                }
            }
        }

        private Cell GetSelectedCell()
        {
            var raycastHit2D = Physics2D.GetRayIntersection(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), Mathf.Infinity);
            if (raycastHit2D.transform != null && raycastHit2D.transform.GetComponent<Cell>() != null)
            {
                return raycastHit2D.transform.GetComponent<Cell>();
            }

            RaycastHit raycastHit3D;
            bool isRaycast3D = Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out raycastHit3D);
            if (isRaycast3D && raycastHit3D.transform.GetComponent<Cell>() != null)
            {
                return raycastHit3D.transform.GetComponent<Cell>();
            }

            return null;
        }

        void GenerateBaseStructure()
        {
            if (GridHelperUtils.CheckMissingParameters(parameterValues))
            {
                return;
            }

            GridHelperUtils.ClearScene(keepMainCamera);

            var mapType = mapTypes[mapTypeIndex];

            cellGrid = new GameObject("CellGrid");
            players = new GameObject("Players");
            units = new GameObject("Units");
            guiController = new GameObject("GUIController");

            directionalLight = new GameObject("DirectionalLight");
            var light = directionalLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.Rotate(45f, 0, 0);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            for (int i = 0; i < nHumanPlayer; i++)
            {
                var player = new GameObject(string.Format("Player_{0}", players.transform.childCount));
                player.AddComponent<HumanPlayer>();
                player.GetComponent<Player>().PlayerNumber = players.transform.childCount;
                player.transform.parent = players.transform;
            }

            for (int i = 0; i < nComputerPlayer; i++)
            {
                var aiPlayer = new GameObject(string.Format("AI_Player_{0}", players.transform.childCount));
                aiPlayer.AddComponent<AIPlayer>();
                aiPlayer.GetComponent<Player>().PlayerNumber = players.transform.childCount;
                aiPlayer.transform.parent = players.transform;
            }

            var cellGridScript = cellGrid.AddComponent<CellGrid>();
            generator.CellsParent = cellGrid.transform;
            generator.Is2D = mapType.Equals(MAP_TYPE_2D);

            cellGrid.GetComponent<CellGrid>().PlayersParent = players.transform;

            var unitGenerator = cellGrid.AddComponent<CustomUnitGenerator>();
            unitGenerator.UnitsParent = units.transform;
            unitGenerator.CellsParent = cellGrid.transform;

            var guiControllerScript = guiController.AddComponent<GUIController>();
            guiControllerScript.CellGrid = cellGridScript;

            cellGrid.AddComponent<SubsequentTurnResolver>();
            cellGrid.AddComponent<DominationCondition>();

            foreach (var fieldName in parameterValues.Keys)
            {
                FieldInfo prop = generator.GetType().GetField(fieldName);
                if (null != prop)
                {
                    prop.SetValue(generator, parameterValues[fieldName]);
                }
            }

            cellGrid.GetComponent<CellGrid>().Is2D = mapType.Equals(MAP_TYPE_2D);
            GridInfo gridInfo = generator.GenerateGrid();

            var camera = Camera.main;
            if (camera == null || !keepMainCamera)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<Camera>();
                camera = cameraObject.GetComponent<Camera>();

                if (useMovableCamera)
                {
                    camera.gameObject.AddComponent<CameraController>();
                    camera.gameObject.GetComponent<CameraController>().ScrollSpeed = cameraScrollSpeed;
                    camera.gameObject.GetComponent<CameraController>().ScrollEdge = cameraScrollEdge;
                }

                camera.transform.position = gridInfo.Center;
                camera.transform.position += mapType.Equals(MAP_TYPE_2D) ? new Vector3(0, 0, -1 * (gridInfo.Dimensions.x > gridInfo.Dimensions.y ? gridInfo.Dimensions.x : gridInfo.Dimensions.y) * (Mathf.Sqrt(3) / 2)) :
                     new Vector3(0, (gridInfo.Dimensions.x > gridInfo.Dimensions.z ? gridInfo.Dimensions.x : gridInfo.Dimensions.z) * (Mathf.Sqrt(3) / 2), 0);
                camera.transform.Rotate(mapType.Equals(MAP_TYPE_2D) ? new Vector3(0, 0, 0) : new Vector3(90, 0, 0));
                camera.transform.SetAsFirstSibling();
            }
        }

        void EnableSceneViewInteraction()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
        }

        void DisableSceneViewInteraction()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeGameObject == null)
            {
                return;
            }

            if (PrefabUtility.GetPrefabAssetType(Selection.activeGameObject) != PrefabAssetType.NotAPrefab)
            {
                if (tileEditModeOn.value || toToggle == tileEditModeOn)
                {
                    if (Selection.activeGameObject.GetComponent<Cell>() != null)
                    {
                        lastPaintedHash = -1;
                        if (PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject) == PrefabInstanceStatus.Connected)
                        {
                            tilePrefab = PrefabUtility.GetCorrespondingObjectFromSource(Selection.activeGameObject).GetComponent<Cell>();
                        }
                        else
                        {
                            tilePrefab = Selection.activeGameObject.GetComponent<Cell>();
                        }
                        Repaint();
                    }
                }

                else if (unitEditModeOn.value || toToggle == unitEditModeOn)
                {
                    if (Selection.activeGameObject.GetComponent<Unit>() != null)
                    {
                        if (PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject) == PrefabInstanceStatus.Connected)
                        {
                            unitPrefab = PrefabUtility.GetCorrespondingObjectFromSource(Selection.activeGameObject).GetComponent<Unit>();
                        }
                        else
                        {
                            unitPrefab = Selection.activeGameObject.GetComponent<Unit>();
                        }
                        Repaint();
                    }
                }
            }
        }

        private void OnUndoPerformed()
        {
            lastPaintedHash = -1;
        }

        internal class BoolWrapper
        {
            public bool value;
            public BoolWrapper(bool value)
            {
                this.value = value;
            }
        }
    }
}