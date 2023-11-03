using HarmonyLib;
using SRML;
using SRML.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using SRML.SR;
using SRML.SR.Translation;
using TMPro;
using System.Json;
using MonomiPark.SlimeRancher.Regions;
using Console = SRML.Console.Console;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AssetsLib
{
    public class Main : ModEntryPoint
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{Environment.CurrentDirectory}\\SRML\\Mods\\{modName}";
        internal static List<Assembly> libAssemblies = new List<Assembly>();

        static Main()
        {
            foreach (var file in modAssembly.GetManifestResourceNames())
                if (file.ToLower().EndsWith(".dll"))
                {
                    try
                    {
                        var stream = modAssembly.GetManifestResourceStream(file);
                        var bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        libAssemblies.Add(Assembly.Load(bytes));
                    }
                    catch (Exception e)
                    {
                        // Log("An error occured loading a resource assembly: " + e);
                    }
                }
            AppDomain.CurrentDomain.AssemblyResolve += (x, y) =>
            {
                var name = new AssemblyName(y.Name).Name;
                foreach (var lA in libAssemblies)
                    if (lA.GetName().Name == name)
                        return lA;
                return null;
            };
            new Harmony("com.aidanamite.AssetsLib").PatchAll();
        }
        static DroneUIProgramPicker _uiPre;
        internal static DroneUIProgramPicker uiPrefab
        {
            get
            {
                if (!_uiPre)
                    _uiPre = Resources.FindObjectsOfTypeAll<DroneUIProgramPicker>().First((x) => !x.name.EndsWith("(Clone)"));
                return _uiPre;
            }
        }
        static DroneUIProgramButton _buttonPre;
        internal static DroneUIProgramButton buttonPrefab
        {
            get
            {
                if (!_buttonPre)
                    _buttonPre = Resources.FindObjectsOfTypeAll<DroneUIProgramButton>().First((x) => x.gameObject.name == "DroneUIProgramButton");
                return _buttonPre;
            }
        }
        static BaseUI _uiPre2;
        internal static BaseUI uiPrefab2
        {
            get
            {
                if (!_uiPre2)
                {
                    var ui = Resources.FindObjectsOfTypeAll<RefineryUI>().First((x) => !x.name.EndsWith("(Clone)"));
                    _buttonPre2 = ui.inventoryEntryPrefab.CreatePrefab();
                    _buttonPre2.GetOrAddComponent<Button>();
                    var ui2 = ui.gameObject.CreatePrefab();
                    var ui3 = ui2.GetComponent<RefineryUI>();
                    _uiPre2 = ui2.AddComponent<BaseUI>();
                    _uiPre2.CopyFields(ui3);
                    Object.DestroyImmediate(ui3);
                }
                return _uiPre2;
            }
        }
        static GameObject _buttonPre2;
        internal static GameObject buttonPrefab2
        {
            get
            {
                if (!_buttonPre2)
                    _ = uiPrefab2;
                return _buttonPre2;
            }
        }
        static InformationUI _uiPre3;
        internal static InformationUI uiPrefab3
        {
            get
            {
                if (!_uiPre3)
                {
                    var ui = Resources.FindObjectsOfTypeAll<PediaUI>().First((x) => !x.name.EndsWith("(Clone)"));
                    var ui3 = ui.CreatePrefab();
                    ui3.gameObject.name = "InformationUI";
                    _uiPre3 = ui3.gameObject.AddComponent<InformationUI>();
                    _uiPre3.CopyFields<BaseUI>(ui3);
                    _uiPre3.listingScroller = ui3.listingScroller;
                    _uiPre3.listingPanel = ui3.listingPanel;
                    _uiPre3.descScroller = ui3.descScroller;
                    _uiPre3.titleText = ui3.titleText;
                    _uiPre3.introText = ui3.introText;
                    _uiPre3.image = ui3.image;
                    _uiPre3.descPanel = ui3.ranchDescPanel;
                    foreach (Transform t in _uiPre3.descPanel.GetAllChildren())
                        Object.DestroyImmediate(t.gameObject);
                    _uiPre3.tabs = ui3.tabs;
                    _uiPre3.tabPrefab = (SRToggle)ui3.ranchTab.CreatePrefab();
                    _uiPre3.tabPrefab.name = "TabButton";
                    _uiPre3.tabPrefab.transform.Find("Text").GetComponent<TMP_Text>().text = "ᓚᘏᗢ";
                    foreach (Transform t in _uiPre3.tabs.transform.GetAllChildren())
                        Object.DestroyImmediate(t.gameObject);
                    _uiPre3.uiHeader = _uiPre3.transform.Find("UIContainer/MainPanel/TitleImage").GetComponent<Image>();
                    _uiPre3.closeButton = _uiPre3.transform.Find("UIContainer/MainPanel/CloseButton").GetComponent<Button>();
                    foreach (Transform t in _uiPre3.descPanel.parent.GetAllChildren())
                        if (t != _uiPre3.descPanel)
                            Object.DestroyImmediate(t.gameObject);
                    Object.DestroyImmediate(ui3);
                }
                return _uiPre3;
            }
        }
        internal static Transform prefabParent;

        public Main()
        {
            var p = new GameObject("PrefabParent");
            p.SetActive(false);
            Object.DontDestroyOnLoad(p);
            prefabParent = p.transform;
        }
    }

    public static class MeshUtils
    {
        /// <summary>
        /// <para>Creates a <see cref="Mesh"/> using the provided vertex, uv and triangle data. For each item in the <paramref name="modifiers"/> a duplicate of the<br/>
        /// mesh, depicted by the data provided, is created, applying the respective modifier to the vertices of the duplicate.</para>
        /// <para>This is designed to be used for generating a single <see cref="Mesh"/> that is made up of several duplicates of another mesh.</para>
        /// <para>Throws an <see cref="ArgumentException"/> if the length of the <paramref name="vertices"/> is different to the length of the <paramref name="uv"/>.<br/>
        /// Throws an <see cref="IndexOutOfRangeException"/> if one of the triangle indices is outside the <paramref name="vertices"/> array.</para>
        /// </summary>
        /// <returns>The generated <see cref="Mesh"/></returns>
        /// <param name="vertices">the array of vertices from the source mesh</param>
        /// <param name="uv">the array of uv points from the source mesh</param>
        /// <param name="triangles">the array of triangle data from the source mesh</param>
        /// <param name="modifiers">the array of modifiers used for each duplicate of the source mesh</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="IndexOutOfRangeException" />
        public static Mesh CreateMesh(Vector3[] vertices, Vector2[] uv, int[] triangles, params Func<Vector3, Vector3>[] modifiers)
        {
            var o = modifiers.Length;
            if (vertices.Length != uv.Length)
                throw new ArgumentException("vertices must be the same length as uv");
            if (triangles.Any((x) => x >= vertices.Length || x < 0))
                throw new IndexOutOfRangeException("all triangle indices must be within the vertices and uv arrays");
            var v = new Vector3[vertices.Length * o];
            var u = new Vector2[uv.Length * o];
            var t = new int[triangles.Length * o];
            for (int i = 0; i < o; i++)
            {
                for (var j = 0; j < vertices.Length; j++)
                {
                    v[i * vertices.Length + j] = modifiers[i](vertices[j]);
                    u[i * vertices.Length + j] = uv[j];
                }
                for (var j = 0; j < triangles.Length; j++)
                    t[i * triangles.Length + j] = triangles[j] + i * vertices.Length;
            }
            var m = new Mesh();
            m.vertices = v;
            m.uv = u;
            m.triangles = t;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
        /// <summary>
        /// <para>Creates a <see cref="Mesh"/> using the provided <see cref="MeshData"/> object. For each item in the modifiers a duplicate of the<br/>
        /// mesh, depicted by the data provided, is created, applying the respective modifier to the duplicate.</para>
        /// <para>This is designed to be used for generating a single <see cref="Mesh"/> that is made up of several duplicates of another mesh.</para>
        /// </summary>
        /// <returns>The generated <see cref="Mesh"/></returns>
        /// <param name="data">a <see cref="MeshData"/> object containing the information required to generate a mesh</param>
        /// <param name="modifiers">the array of modifiers used for each duplicate of the source mesh</param>
        public static Mesh CreateMesh(MeshData data, params MeshModifier[] modifiers)
        {
            var r = MeshData.Empty;
            foreach (var m in modifiers)
                r += data * m;
            return r;
        }
        /// <summary>
        /// <para>Creates a single <see cref="Mesh"/> by combining the contents of all the provided <see cref="MeshData"/> objects</para>
        /// </summary>
        /// <returns>The generated <see cref="Mesh"/></returns>
        /// <param name="datas">an array of <see cref="MeshData"/> objects to be combined</param>
        public static Mesh CreateMesh(params MeshData[] datas)
        {
            var r = MeshData.Empty;
            foreach (var d in datas)
                r += d;
            return r;
        }

        /// <summary>
        /// <para>Creates a <see cref="Mesh"/> using the provided vertex, uv and triangle data. Points in the mesh that match the<br/>
        /// <paramref name="removeAt"/> predicate will be removed, all other points will be modified by the specified modify function</para>
        /// <para>This is designed to be used for tweaking a <see cref="Mesh"/></para>
        /// <para>Throws an <see cref="ArgumentException"/> if the length of the <paramref name="vertices"/> is different to the length of the <paramref name="uv"/>.<br/>
        /// Throws an <see cref="IndexOutOfRangeException"/> if one of the triangle indices is outside the <paramref name="vertices"/> array.</para>
        /// </summary>
        /// <returns>The generated <see cref="Mesh"/></returns>
        /// <param name="vertices">the array of vertices from the source mesh</param>
        /// <param name="uv">the array of uv points from the source mesh</param>
        /// <param name="triangles">the array of triangle data from the source mesh</param>
        /// <param name="removeAt">the condition for removing a vertex from the mesh</param>
        /// <param name="modify">the change that should occur to each vertex in the mesh</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="IndexOutOfRangeException" />
        public static Mesh CreateMesh(IEnumerable<Vector3> vertices, IEnumerable<int> triangles, IEnumerable<Vector2> uv, Predicate<Vector3> removeAt, Func<Vector3, Vector3> modify)
        {
            var m = new Mesh();
            var v = vertices.ToList();
            var t = triangles.ToList();
            var u = uv.ToList();
            if (v.Count != u.Count)
                throw new ArgumentException("vertices must be the same length as uv");
            if (triangles.Any((x) => x >= v.Count || x < 0))
                throw new IndexOutOfRangeException("all triangle indices must be within the vertices and uv arrays");
            for (int i = v.Count - 1; i >= 0; i--)
            {
                if (removeAt(v[i]))
                {
                    v.RemoveAt(i);
                    u.RemoveAt(i);
                    for (int j = t.Count - 3; j >= 0; j -= 3)
                    {
                        if (t[j] == i || t[j + 1] == i || t[j + 2] == i)
                        {
                            t.RemoveRange(j, 3);
                            continue;
                        }
                        if (t[j] > i)
                            t[j]--;
                        if (t[j + 1] > i)
                            t[j + 1]--;
                        if (t[j + 2] > i)
                            t[j + 2]--;
                    }
                }
                else
                    v[i] = modify(v[i]);
            }
            m.vertices = v.ToArray();
            m.triangles = t.ToArray();
            m.uv = u.ToArray();
            m.RecalculateBounds();
            m.RecalculateNormals();
            m.RecalculateTangents();
            return m;
        }

        /// <summary>
        /// <para>Generates a basic set of bone data for provided <see cref="Mesh"/>es and <see cref="SlimeAppearanceObject"/>s</para>
        /// <para>This is designed to be used for generating the bone weights and configuring the<br/>
        /// <see cref="SlimeAppearanceObject"/>s for a custom slime model</para>
        /// <para>Note: this only works for <see cref="SlimeAppearanceObject"/>s that use a <see cref="SkinnedMeshRenderer"/>.<br/>
        /// <see cref="MeshFilter"/>s will not be affected</para>
        /// </summary>
        /// <param name="slimePrefab">the <see cref="SlimeAppearanceApplicator"/> from your slime prefab</param>
        /// <param name="bodyApp">the <see cref="SlimeAppearanceObject"/> of your slime's main body</param>
        /// <param name="jiggleAmount">the amount the model will be affected by the slime's movement</param>
        /// <param name="scale">the scale to put the model to. This is handy due to model's scale being unaffected by <see cref="Transform.localScale"/></param>
        public static void GenerateBoneData(SlimeAppearanceApplicator slimePrefab, SlimeAppearanceObject bodyApp, float jiggleAmount = 1, float scale = 1) => GenerateBoneData(slimePrefab, bodyApp, jiggleAmount, scale, appearanceObjects: null, AdditionalMesh: null);

        /// <summary>
        /// <para>Generates a basic set of bone data for provided <see cref="Mesh"/>es and <see cref="SlimeAppearanceObject"/>s</para>
        /// <para>This is designed to be used for generating the bone weights and configuring the<br/>
        /// <see cref="SlimeAppearanceObject"/>s for a custom slime model</para>
        /// <para>Note: this only works for <see cref="SlimeAppearanceObject"/>s that use a <see cref="SkinnedMeshRenderer"/>.<br/>
        /// <see cref="MeshFilter"/>s will not be affected</para>
        /// </summary>
        /// <param name="slimePrefab">the <see cref="SlimeAppearanceApplicator"/> from your slime prefab</param>
        /// <param name="bodyApp">the <see cref="SlimeAppearanceObject"/> of your slime's main body</param>
        /// <param name="jiggleAmount">the amount the model will be affected by the slime's movement</param>
        /// <param name="scale">the scale to put the model to. This is handy due to model's scale being unaffected by <see cref="Transform.localScale"/></param>
        /// <param name="appearanceObjects">the additional <see cref="SlimeAppearanceObject"/>s that you want the weight data and configuration to be put on</param>
        public static void GenerateBoneData(SlimeAppearanceApplicator slimePrefab, SlimeAppearanceObject bodyApp, float jiggleAmount = 1, float scale = 1, params SlimeAppearanceObject[] appearanceObjects) => GenerateBoneData(slimePrefab, bodyApp, jiggleAmount, scale, null, appearanceObjects);

        /// <summary>
        /// <para>Generates a basic set of bone data for provided <see cref="Mesh"/>es and <see cref="SlimeAppearanceObject"/>s</para>
        /// <para>This is designed to be used for generating the bone weights and configuring the<br/>
        /// <see cref="SlimeAppearanceObject"/>s for a custom slime model</para>
        /// <para>Note: this only works for <see cref="SlimeAppearanceObject"/>s that use a <see cref="SkinnedMeshRenderer"/>.<br/>
        /// <see cref="MeshFilter"/>s will not be affected</para>
        /// </summary>
        /// <param name="slimePrefab">the <see cref="SlimeAppearanceApplicator"/> from your slime prefab</param>
        /// <param name="bodyApp">the <see cref="SlimeAppearanceObject"/> of your slime's main body</param>
        /// <param name="jiggleAmount">the amount the model will be affected by the slime's movement</param>
        /// <param name="scale">the scale to put the model to. This is handy due to model's scale being unaffected by <see cref="Transform.localScale"/></param>
        /// <param name="AdditionalMesh">an array of additional <see cref="Mesh"/>es that you want the weight data to be put on</param>
        public static void GenerateBoneData(SlimeAppearanceApplicator slimePrefab, SlimeAppearanceObject bodyApp, float jiggleAmount = 1, float scale = 1, params Mesh[] AdditionalMesh) => GenerateBoneData(slimePrefab, bodyApp, jiggleAmount, scale, AdditionalMesh, null);

        /// <summary>
        /// <para>Generates a basic set of bone data for provided <see cref="Mesh"/>es and <see cref="SlimeAppearanceObject"/>s</para>
        /// <para>This is designed to be used for generating the bone weights and configuring the<br/>
        /// <see cref="SlimeAppearanceObject"/>s for a custom slime model</para>
        /// <para>Note: this only works for <see cref="SlimeAppearanceObject"/>s that use a <see cref="SkinnedMeshRenderer"/>.<br/>
        /// <see cref="MeshFilter"/>s will not be affected</para>
        /// </summary>
        /// <param name="slimePrefab">the <see cref="SlimeAppearanceApplicator"/> from your slime prefab</param>
        /// <param name="appearance">the slime appearance of your slime, the first valid object is assumed to be the slime's body mesh</param>
        /// <param name="jiggleAmount">the amount the model will be affected by the slime's movement</param>
        /// <param name="scale">the scale to put the model to. This is handy due to model's scale being unaffected by <see cref="Transform.localScale"/></param>
        /// <param name="AdditionalMesh">an array of additional <see cref="Mesh"/>es that you want the weight data to be put on</param>
        public static void GenerateBoneData(SlimeAppearanceApplicator slimePrefab, SlimeAppearance appearance, float jiggleAmount = 1, float scale = 1, params Mesh[] AdditionalMesh)
        {
            var objs = new List<SlimeAppearanceObject>();
            foreach (var s in appearance.Structures)
            {
                if (s == null)
                    throw new NullReferenceException("One or more of the SlimeAppearanceStructures is null");
                if (!s.Element)
                    throw new NullReferenceException("One or more of the SlimeAppearanceElements is null");
                if ( s.Element.Prefabs == null)
                    throw new NullReferenceException("One or more of the SlimeAppearanceElement's prefab arrays are null");
                foreach (var o in s.Element.Prefabs)
                        if (o && o.GetComponent<Renderer>() is SkinnedMeshRenderer && !objs.Contains(o))
                            objs.Add(o);
            }
            if (objs.Count == 0)
                throw new ArgumentException("The provided SlimeAppearance does not contain any SkinnedMeshRenderers", "appearance");
            var body = objs.First();
            objs.RemoveAt(0);
            GenerateBoneData(slimePrefab, body, jiggleAmount, scale, AdditionalMesh, objs.ToArray());
        }

        /// <summary>
        /// <para>Generates a basic set of bone data for provided <see cref="Mesh"/>es and <see cref="SlimeAppearanceObject"/>s</para>
        /// <para>This is designed to be used for generating the bone weights and configuring the<br/>
        /// <see cref="SlimeAppearanceObject"/>s for a custom slime model</para>
        /// <para>Note: this only works for <see cref="SlimeAppearanceObject"/>s that use a <see cref="SkinnedMeshRenderer"/>.<br/>
        /// <see cref="MeshFilter"/>s will not be affected</para>
        /// </summary>
        /// <param name="slimePrefab">the <see cref="SlimeAppearanceApplicator"/> from your slime prefab</param>
        /// <param name="bodyApp">the <see cref="SlimeAppearanceObject"/> of your slime's main body</param>
        /// <param name="jiggleAmount">the amount the model will be affected by the slime's movement</param>
        /// <param name="scale">the scale to put the model to. This is handy due to model's scale being unaffected by <see cref="Transform.localScale"/></param>
        /// <param name="AdditionalMesh">an array of additional <see cref="Mesh"/>es that you want the weight data to be put on</param>
        /// <param name="appearanceObjects">the additional <see cref="SlimeAppearanceObject"/>s that you want the weight data and configuration to be put on</param>
        public static void GenerateBoneData(SlimeAppearanceApplicator slimePrefab, SlimeAppearanceObject bodyApp, float jiggleAmount = 1, float scale = 1, Mesh[] AdditionalMesh = null, params SlimeAppearanceObject[] appearanceObjects)
        {
            if (!slimePrefab)
                throw new ArgumentNullException("slimePrefab");
            if (!bodyApp)
                throw new ArgumentNullException("bodyApp");
            if (AdditionalMesh == null)
                AdditionalMesh = new Mesh[0];
            if (appearanceObjects == null)
                appearanceObjects = new SlimeAppearanceObject[0];
            var mesh = bodyApp.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            bodyApp.AttachedBones = new SlimeAppearance.SlimeBone[] { SlimeAppearance.SlimeBone.Slime, SlimeAppearance.SlimeBone.JiggleRight, SlimeAppearance.SlimeBone.JiggleLeft, SlimeAppearance.SlimeBone.JiggleTop, SlimeAppearance.SlimeBone.JiggleBottom, SlimeAppearance.SlimeBone.JiggleFront, SlimeAppearance.SlimeBone.JiggleBack };
            foreach (var a in appearanceObjects)
                if (a)
                    a.AttachedBones = bodyApp.AttachedBones;
                else
                    throw new NullReferenceException("One or more of the SlimeAppearanceObjects are null");
            var v = mesh.vertices;
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var sum = Vector3.zero;
            for (int i = 0; i < v.Length; i++)
            {
                sum += v[i];
                if (v[i].x > max.x)
                    max.x = v[i].x;
                if (v[i].x < min.x)
                    min.x = v[i].x;
                if (v[i].y > max.y)
                    max.y = v[i].y;
                if (v[i].y < min.y)
                    min.y = v[i].y;
                if (v[i].z > max.z)
                    max.z = v[i].z;
                if (v[i].z < min.z)
                    min.z = v[i].z;
            }
            var center = sum / v.Length;
            var dis = 0f;
            foreach (var ver in v)
                dis += (ver - center).magnitude;
            dis /= v.Length;
            var meshes = new List<Mesh>() { mesh };
            foreach (var x in appearanceObjects)
                if (x.GetComponent<SkinnedMeshRenderer>())
                    meshes.Add(x.GetComponent<SkinnedMeshRenderer>().sharedMesh);
                else
                    Debug.LogWarning("One of the SlimeAppearanceObjects provided to AssetsLib.MeshUtils.GenerateBoneData does not use a SkinnedMeshRenderer");
            meshes.AddRange(AdditionalMesh);
            foreach (var m in meshes)
            {
                if (!m)
                {
                    Debug.LogWarning("One of the Meshes provided to AssetsLib.MeshUtils.GenerateBoneData is null");
                    continue;
                }
                var v2 = m.vertices;
                var b = new BoneWeight[v2.Length];
                for (int i = 0; i < v2.Length; i++)
                {
                    var r = v2[i] - center;
                    var o = Mathf.Clamp01((r.magnitude - (dis / 4)) / (dis / 2) * jiggleAmount);
                    b[i] = new BoneWeight();
                    if (o == 0)
                        b[i].weight0 = 1;
                    else
                    {
                        b[i].weight0 = 1 - o;
                        b[i].boneIndex1 = r.x >= 0 ? 1 : 2;
                        b[i].boneIndex2 = r.y >= 0 ? 3 : 4;
                        b[i].boneIndex3 = r.z >= 0 ? 5 : 6;
                        var n = r.Multiply(r).Multiply(r).Abs();
                        var s = n.ToArray().Sum();
                        b[i].weight1 = n.x / s * o;
                        b[i].weight2 = n.y / s * o;
                        b[i].weight3 = n.z / s * o;
                    }
                    b[i].weight0 *= scale;
                    b[i].weight1 *= scale;
                    b[i].weight2 *= scale;
                    b[i].weight3 *= scale;
                }
                m.boneWeights = b;

                var p = new Matrix4x4[bodyApp.AttachedBones.Length];
                for (int i = 0; i < bodyApp.AttachedBones.Length; i++)
                    p[i] = slimePrefab.Bones.First((x) => x.Bone == bodyApp.AttachedBones[i]).BoneObject.transform.worldToLocalMatrix * slimePrefab.Bones.First((x) => x.Bone == SlimeAppearance.SlimeBone.Root).BoneObject.transform.localToWorldMatrix;
                m.bindposes = p;
            }
        }
    }

    public static class TextureUtils
    {
        /// <summary>
        /// <para>Loads an image from the embedded resources.</para>
        /// <para>Throws a <see cref="MissingResourceException"/> if no file is found under the specified name.</para>
        /// </summary>
        /// <returns>The loaded image</returns>
        /// <param name="filename">the name of the file to load (including extention)</param>
        /// <exception cref="MissingResourceException"/>
        public static Texture2D LoadImage(string filename, FilterMode mode = FilterMode.Bilinear, TextureWrapMode wrapMode = default)
        {
            var modAssembly = Assembly.GetCallingAssembly();
            filename = modAssembly.GetName().Name + "." + filename;
            var spriteData = modAssembly.GetManifestResourceStream(filename);
            if (spriteData == null)
                throw new MissingResourceException(filename);
            var rawData = new byte[spriteData.Length];
            spriteData.Read(rawData, 0, rawData.Length);
            var tex = new Texture2D(1, 1);
            tex.LoadImage(rawData);
            tex.filterMode = mode;
            tex.wrapMode = wrapMode;
            tex.name = System.IO.Path.GetFileNameWithoutExtension(filename);
            return tex;
        }

        /// <summary>
        /// <para>Creates a basic <see cref="Sprite"/> from the <see cref="Texture2D"/></para>
        /// </summary>
        /// <returns>The created <see cref="Sprite"/> object</returns>
        public static Sprite CreateSprite(this Texture2D texture)
        {
            var s = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1);
            s.name = texture.name;
            return s;
        }

        /// <returns>Creates a readable copy of the <see cref="Sprite"/>.</returns>
        public static Sprite GetReadable(this Sprite source)
        {
            return Sprite.Create(source.texture.GetReadable(), source.rect, source.pivot, source.pixelsPerUnit);
        }

        /// <returns>Creates a readable copy of the Texture2D</returns>
        public static Texture2D GetReadable(this Texture2D source)
        {
            RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, temp);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = temp;
            Texture2D texture = new Texture2D(source.width, source.height);
            texture.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
            texture.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(temp);
            return texture;
        }

        /// <returns>Creates a readable copy of the <see cref="Cubemap"/>.</returns>
        public static Cubemap GetReadable(this Cubemap source)
        {
            Cubemap texture = new Cubemap(source.width, TextureFormat.RGBA32, source.mipmapCount);
            Graphics.CopyTexture(source, texture);
            return texture;
        }

        /// <summary>Edits the pixels on a <see cref="Texture2D"/>.</summary>
        public static void ModifyTexturePixels(this Texture2D texture, Func<Color, Color> colorChange)
        {
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                var p = texture.GetPixels(m);
                for (int x = 0; x < p.Length; x++)
                    p[x] = colorChange(p[x]);
                texture.SetPixels(p, m);
            }
            texture.Apply(true);
        }

        /// <summary>Edits the pixels on a <see cref="Texture2D"/>.</summary>
        public static void ModifyTexturePixels(this Texture2D texture, Func<Color, float, float, Color> colorChange)
        {
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                var p = texture.GetPixels(m);
                var w = Mathf.Max(1, texture.width >> m);
                var h = p.Length / w;
                for (int x = 0; x < p.Length; x++)
                    p[x] = colorChange(p[x], (x % w + 1) / (float)w, (x / w + 1) / (float)h);
                texture.SetPixels(p, m);
            }
            texture.Apply(true);
        }

        /// <summary>Edits the pixels on a <see cref="Cubemap"/>.</summary>
        public static void ModifyTexturePixels(this Cubemap texture, Func<Color, Color> colorChange)
        {
            var p = new Color[texture.mipmapCount][][];
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                p[m] = new Color[6][];
                for (CubemapFace f = 0; f <= CubemapFace.NegativeZ; f++)
                    p[m][(int)f] = texture.GetPixels(f, 0);
            }
            for (int m = 0; m < texture.mipmapCount; m++)
                for (CubemapFace f = 0; f <= CubemapFace.NegativeZ; f++)
                {
                    for (int x = 0; x < p[m][(int)f].Length; x++)
                        p[m][(int)f][x] = colorChange(p[m][(int)f][x]);
                    texture.SetPixels(p[m][(int)f], f, m);
                }
            texture.Apply();
        }

        /// <summary>Edits the pixels on a <see cref="Cubemap"/>.</summary>
        public static void ModifyTexturePixels(this Cubemap texture, Func<Color, CubemapFace, float, float, Color> colorChange)
        {
            var p = new Color[texture.mipmapCount][][];
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                p[m] = new Color[6][];
                for (CubemapFace f = 0; f <= CubemapFace.NegativeZ; f++)
                    p[m][(int)f] = texture.GetPixels(f, m);
            }
            for (int m = 0; m < texture.mipmapCount; m++)
                for (CubemapFace f = 0; f <= CubemapFace.NegativeZ; f++)
                {
                    var w = Mathf.Max(1, texture.width >> m);
                    for (int x = 0; x < p[m][(int)f].Length; x++)
                        p[m][(int)f][x] = colorChange(p[m][(int)f][x], f, (x % w + 1) / (float)w, (x / w + 1) / (float)w);
                    texture.SetPixels(p[m][(int)f], f, m);
                }
            texture.Apply();
        }
    }

    public static class UIUtils
    {
        /// <summary>
        /// <para>Opens a UI similar to the one used for drone program selection</para>
        /// </summary>
        /// <returns>The <see cref="GameObject"/> of the opened ui</returns>
        /// <param name="titleKey">the UI name key to display at the top of the UI</param>
        /// <param name="titleIcon">the icon to show at the top of the UI</param>
        /// <param name="options">the options to display in the UI</param>
        /// <param name="closeMenuOnSelect">whether or not the UI should close upon selecting an option</param>
        /// <param name="onClose">the code to be run when the UI is closed</param>
        public static GameObject CreateSelectionUI(string titleKey, Sprite titleIcon, List<ModeOption> options, bool closeMenuOnSelect = false, Action onClose = null)
        {
            var ui = Object.Instantiate(Main.uiPrefab);
            ui.title.text = GameContext.Instance.MessageDirector.Get("ui", titleKey);
            ui.icon.sprite = titleIcon;
            List<Button> buttons = new List<Button>();
            foreach (var option in options)
            {
                var button = Object.Instantiate(Main.buttonPrefab, ui.contentGrid).Init(option);
                button.button.onClick.AddListener(() => {
                    if (closeMenuOnSelect)
                        ui.Close();
                    option.Selected();
                    if (!closeMenuOnSelect)
                        button.Init(option);
                });
                buttons.Add(button.button);
                if (buttons.Count == 1)
                    button.button.gameObject.AddComponent<InitSelected>();
            }
            int num = Mathf.CeilToInt(buttons.Count / 6f);
            for (int j = 0; j < buttons.Count; j++)
            {
                int y = j / 6;
                int x = j % 6;
                Navigation navigation = buttons[j].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                if (y > 0)
                    navigation.selectOnUp = buttons[(y - 1) * 6 + x];
                if (y < num - 1)
                    navigation.selectOnDown = buttons[Mathf.Min((y + 1) * 6 + x, buttons.Count - 1)];
                if (x > 0)
                    navigation.selectOnLeft = buttons[y * 6 + (x - 1)];
                if (x < 5 && j < buttons.Count - 1)
                    navigation.selectOnRight = buttons[y * 6 + (x + 1)];
                buttons[j].navigation = navigation;
            }
            if (onClose != null)
                ui.onDestroy += () => onClose();
            return ui.gameObject;
        }

        /// <summary>
        /// Designed for use with a <see cref="PurchaseUI"/>
        /// </summary>
        /// <param name="ui">the <see cref="GameObject"/> of the UI</param>
        /// <param name="refresh">if <see langword="true"/>, the UI is refreshed on purchase. if <see langword="false"/> the UI is closed on purchase</param>
        /// <param name="action">the code to run on successful purchase</param>
        /// <param name="cost">the amount of money required to purchase the item (takes the money away from the player on purchase)</param>
        public static void Purchase(GameObject ui, bool refresh, Action action, int cost)
        {
            var playerState = SRSingleton<SceneContext>.Instance.PlayerState;
            if (playerState.GetCurrency() >= cost)
            {
                playerState.SpendCurrency(cost, false);
                action?.Invoke();
                if (ui)
                {
                    var u = ui.GetComponent<PurchaseUI>();
                    u.PlayPurchaseFX();
                    if (refresh)
                        u.Rebuild(false);
                    else
                        u.Close();
                }
            }
        }

        /// <summary>Creates a basic inventory UI</summary>
        /// <param name="titleKey">the UI name key to display at the top of the UI</param>
        /// <param name="titleIcon">the icon to show at the top of the UI</param>
        /// <param name="options">the items to display in the ui</param>
        /// <param name="closeMenuOnSelect">whether or not the UI should close upon selecting an option</param>
        /// <param name="onClose">the code to be run when the UI is closed</param>
        /// <returns>The <see cref="GameObject"/> of the opened ui</returns>
        public static GameObject CreateInventoryUI(string titleKey, Sprite titleIcon, IEnumerable<IInventoryItem> options, bool closeMenuOnSelect = false, Action onClose = null)
        {
            var ui = Object.Instantiate(Main.uiPrefab2);
            var title = ui.transform.Find("MainPanel/TitlePanel/Title").GetComponent<XlateText>();
            title.bundlePath = "ui";
            title.SetKey(titleKey);
            ui.transform.Find("MainPanel/TitlePanel/TitleIcon").GetComponent<Image>().sprite = titleIcon;
            MessageDirector.BundlesListener onBundleChange = null;
            ui.onDestroy += () => {
                GameContext.Instance.MessageDirector.bundlesListeners -= onBundleChange;
                onClose?.Invoke();
            };
            ui.transform.Find("MainPanel/CloseButton").GetComponent<Button>().onClick.AddListener(ui.Close);
            var p = ui.transform.Find("MainPanel/MainBody/InventoryPanel/Content/InventoryGrid");
            foreach (var option in options) {
                var e = Object.Instantiate(Main.buttonPrefab2, p, false);
                option.RefreshEntry(e);
                var b = e.GetComponent<Button>(); // move to prefab init
                b.onClick.AddListener(() =>
                {
                    if (option.OnClick())
                    {
                        if (closeMenuOnSelect)
                            ui.Close();
                        else
                            option.RefreshEntry(e);
                    }
                });
                onBundleChange += (x) =>
                {
                    option.RefreshEntry(e);
                };
            }
            GameContext.Instance.MessageDirector.bundlesListeners += onBundleChange;
            return ui.gameObject;
        }

        /// <summary>Creates a basic information UI</summary>
        /// <param name="TitleImage">the image to display at the top of the UI</param>
        /// <param name="Tabs">the tabs to put in the UI</param>
        /// <param name="onClose">the code to be run when the UI is closed</param>
        /// <param name="DefaultSelection">the item to have selected when the UI opens</param>
        /// <returns>The <see cref="GameObject"/> of the opened ui</returns>
        public static GameObject CreateInformationUI(Sprite TitleImage, IEnumerable<InformationTab> Tabs, Action onClose = null, InformationItem DefaultSelection = null)
        {
            var ui = Object.Instantiate(Main.uiPrefab3);
            ui.uiHeader.sprite = TitleImage;
            ui.onDestroy += () =>
            {
                onClose?.Invoke();
            };
            ui.SetItems(Tabs,DefaultSelection);
            return ui.gameObject;
        }
    }
    public class InformationUI : BaseUI
    {
        public RectTransform listingPanel;
        public ScrollRect listingScroller;
        public ScrollRect descScroller;
        public TMP_Text titleText;
        public TMP_Text introText;
        public Image image;
        public RectTransform descPanel;
        public TMP_Text descTextPrefab;
        public TabByMenuKeys tabs;
        public SRToggle tabPrefab;
        public Image uiHeader;
        public Button closeButton;
        Dictionary<InformationTab, (SRToggle, List<(InformationItem, Toggle)>)> tabItems = new Dictionary<InformationTab, (SRToggle, List<(InformationItem, Toggle)>)>();
        public override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
        }
        public void SetItems(IEnumerable<InformationTab> Items, InformationItem DefaultSelection = null)
        {
            InformationTab Selected = null;
            var bundle = GameContext.Instance.MessageDirector.GetBundle("ui");
            foreach (var t in tabItems)
            {
                Destroy(t.Value.Item1.gameObject);
                foreach (var i in t.Value.Item2)
                    Destroy(i.Item2.gameObject);
            }
            tabItems.Clear();
            foreach(var t in Items)
                if (t != null)
            {
                var toggle = Instantiate(tabPrefab, tabs.transform);
                toggle.transform.Find("Text").GetComponent<TMP_Text>().text = bundle.Get(t.NameKey);
                toggle.onValueChanged.AddListener(x =>
                {
                    if (x)
                    {
                        listingScroller.verticalNormalizedPosition = 1f;
                        SelectTab(t);
                    }
                });
                toggle.gameObject.SetActive(t.Available?.Invoke() ?? true);
                var items = new List<(InformationItem, Toggle)>();
                foreach (var i in t.Items)
                    if (i != null)
                {
                    var item = Instantiate(SceneContext.Instance.PediaDirector.pediaListingPrefab, listingPanel);
                    item.transform.Find("NameText").GetComponent<TMP_Text>().text = bundle.Get(i.TitleKey);
                    item.transform.Find("Image").GetComponent<Image>().sprite = i.Icon;
                    OnSelectDelegator.Create(item, () => SelectItem(t, i));
                    if (DefaultSelection == i)
                        Selected = t;
                    items.Add((i, item.GetComponent<Toggle>()));
                }
                tabItems.Add(t, (toggle, items));
            }
            tabs.Awake();
            if (Selected == null)
                SelectTab(Items.First());
            else
                SelectItem(Selected, DefaultSelection);
        }
        public void SelectItem(InformationItem Item, bool LogFail = true)
        {
            var tab = tabItems.FirstOrDefault(x => x.Value.Item2.Exists(y => y.Item1 == Item));
            if (tab.Key == null)
            {
                if (LogFail)
                    Debug.LogWarning($"Could not find item {GameContext.Instance.MessageDirector.Get("ui", Item.TitleKey)} in the InformationUI");
            }
            else
                SelectItem(tab.Key, Item);
        }
        public void SelectTab(InformationTab Tab) => SelectItem(Tab, Tab.Items[0]);
        public void SelectItem(InformationTab Tab, InformationItem Item)
        {
            var bundle = GameContext.Instance.MessageDirector.GetBundle("ui");
            foreach (var t in tabItems)
            {
                var flag = t.Key == Tab;
                if (t.Value.Item1.isOn != flag)
                    t.Value.Item1.isOn = flag;
                foreach (var i in t.Value.Item2)
                {
                    var flag2 = flag && i.Item1 == Item;
                    if (i.Item2.isOn != flag2)
                    {
                        i.Item2.isOn = flag2;
                        if (flag2)
                        {
                            i.Item2.Select();
                            titleText.text = bundle.Get(i.Item1.TitleKey);
                            introText.text = bundle.Get(i.Item1.DescKey);
                            image.sprite = i.Item1.Icon;
                            descPanel.gameObject.SetActive(false);
                            descPanel.gameObject.SetActive(true);
                            foreach (Transform o in descPanel.GetAllChildren())
                                DestroyImmediate(o.gameObject);
                            foreach (var c in i.Item1.Contents)
                                if (c != null)
                                    CreateText(bundle.Get(c.TextKey,c.Params),c.FormatName).transform.SetParent(descPanel, false);
                            descScroller.verticalNormalizedPosition = 1;
                        }
                    }
                    var flag3 = flag && (i.Item1.Available?.Invoke() ?? true);
                    if (i.Item2.gameObject.activeSelf != flag3)
                        i.Item2.gameObject.SetActive(flag3);
                }
            }
        }
        public static GameObject CreateText(string Text, string Format)
        {
            var g = new GameObject("Text");
            g.AddComponent<TextMeshProUGUI>().text = Text;
            g.AddComponent<MeshTextStyler>().SetStyle(Format);
            return g.gameObject;
        }
    }

    /// <summary>For use with <seealso cref="UIUtils.CreateInformationUI(Sprite, IEnumerable{InformationTab}, Action, InformationItem)"/></summary>
    public class InformationTab
    {
        public string NameKey;
        public InformationItem[] Items;
        public Func<bool> Available;
        public InformationTab(string nameKey, InformationItem[] items)
        {
            NameKey = nameKey;
            Items = items;
        }
    }

    /// <summary>For use with <seealso cref="UIUtils.CreateInformationUI(Sprite, IEnumerable{InformationTab}, Action, InformationItem)"/></summary>
    public class InformationItem
    {
        public string TitleKey;
        public string DescKey;
        public Sprite Icon;
        public ContentItem[] Contents;
        public Func<bool> Available;
        public InformationItem(string titleKey, string descKey, Sprite icon, ContentItem[] contents)
        {
            TitleKey = titleKey;
            DescKey = descKey;
            Icon = icon;
            Contents = contents;
        }
    }

    /// <summary>For use with <seealso cref="UIUtils.CreateInformationUI(Sprite, IEnumerable{InformationTab}, Action, InformationItem)"/></summary>
    public class ContentItem
    {
        public string TextKey;
        public object[] Params;
        public string FormatName;
        public const string HeaderFormat = "LargeBold";
        public const string DefaultFormat = "Default";
        public ContentItem(string textKey, string formatName, params object[] parameters)
        {
            TextKey = textKey;
            FormatName = formatName;
            Params = parameters;
        }
        public static ContentItem CreateNormalItem(string textKey, params object[] parameters) => new ContentItem(textKey, DefaultFormat, parameters);
        public static ContentItem CreateHeaderItem(string textKey, params object[] parameters) => new ContentItem(textKey, HeaderFormat, parameters);
    }

    public interface IInventoryItem
    {
        bool OnClick();
        void RefreshEntry(GameObject gO);
    }

    /// <summary>A basic inventory item that use the name and icon from an <see cref="Identifiable.Id"/></summary>
    public class IdentInventoryItem : IInventoryItem
    {
        Identifiable.Id id;
        int count;
        Func<int> getCount;
        Func<bool> onClick;
        public IdentInventoryItem(Identifiable.Id Id, int Count, Func<bool> OnClick = null)
        {
            id = Id;
            count = Count;
            onClick = OnClick;
        }
        public IdentInventoryItem(Identifiable.Id Id, Func<int> Count, Func<bool> OnClick = null)
        {
            id = Id;
            getCount = Count;
            onClick = OnClick;
        }
        bool IInventoryItem.OnClick() => onClick?.Invoke() ?? false;
        void IInventoryItem.RefreshEntry(GameObject gO)
        {
            var count = getCount?.Invoke() ?? this.count;
            if (id == Identifiable.Id.NONE)
                gO.transform.Find("Content/Name").GetComponent<TMP_Text>().text = GameContext.Instance.MessageDirector.Get("pedia", "t.locked");
            else
            {
                gO.transform.Find("Content/Name").GetComponent<TMP_Text>().text = GameContext.Instance.MessageDirector.Get("actor","l." + id.ToString().ToLowerInvariant());
                gO.transform.Find("Content/Icon").GetComponent<Image>().sprite = GameContext.Instance.LookupDirector.GetIcon(id);
            }
            gO.transform.Find("CountsOuterPanel/CountsPanel/Counts").GetComponent<TMP_Text>().text = ((count > 999) ? string.Format("{0}+", 999) : count.ToString());
        }
    }

    /// <summary>A generic custom inventory item</summary>
    public class GenericInventoryItem : IInventoryItem
    {
        string nameKey;
        Func<Sprite> sprite;
        Func<string> count;
        Func<bool> onClick;
        public GenericInventoryItem(string NameKey, Func<Sprite> Sprite, Func<string> Count, Func<bool> OnClick = null)
        {
            nameKey = NameKey;
            sprite = Sprite;
            count = Count;
            onClick = OnClick;
        }
        public GenericInventoryItem(string NameKey, Sprite Sprite, Func<string> Count, Func<bool> OnClick = null) : this(NameKey, () => Sprite, Count, OnClick) { }
        public GenericInventoryItem(string NameKey, Func<Sprite> Sprite, string Count, Func<bool> OnClick = null) : this(NameKey, Sprite, () => Count, OnClick) { }
        public GenericInventoryItem(string NameKey, Sprite Sprite, string Count, Func<bool> OnClick = null) : this(NameKey, () => Sprite, () => Count, OnClick) { }
        bool IInventoryItem.OnClick() => onClick?.Invoke() ?? false;
        void IInventoryItem.RefreshEntry(GameObject gO)
        {
            gO.transform.Find("Content/Name").GetComponent<TMP_Text>().text = GameContext.Instance.MessageDirector.Get("ui", nameKey);
            gO.transform.Find("Content/Icon").GetComponent<Image>().sprite = sprite();
            gO.transform.Find("CountsOuterPanel/CountsPanel/Counts").GetComponent<TMP_Text>().text = count();
        }
    }


    public static class GameObjectUtils
    {
        /// <summary>Finds an effect prefab given its <paramref name="name"/></summary>
        /// <returns>The effect prefab</returns>
        /// <param name="name">the name of the effect prefab to search for</param>
        public static GameObject FindFX(string name) => SceneContext.Instance.fxPool.pooledObjects.Keys.First((x) => x.name == name);

        /// <summary>Finds an effect prefab given a <see cref="Predicate{T}"/></summary>
        /// <returns>The effect prefab</returns>
        /// <param name="predicate">the predicate to search for a match for</param>
        public static GameObject FindFX(Predicate<GameObject> predicate) => SceneContext.Instance.fxPool.pooledObjects.Keys.First((x) => predicate(x));

        /// <summary>Finds an object of type <typeparamref name="T"/> given its <paramref name="name"/></summary>
        /// <returns>The first object found</returns>
        /// <param name="name">the name of the object to search for</param>
        public static T FindObjectByName<T>(string name) where T : Object => Resources.FindObjectsOfTypeAll<T>().FirstOrDefault((x) => x.name == name);

        /// <summary>Finds multi objects of type <typeparamref name="T"/> given their <paramref name="names"/></summary>
        /// <returns>An array of the first object found with each name</returns>
        /// <param name="names">the names of the objects to search for</param>
        public static T[] FindObjectsByNames<T>(params string[] names) where T : Object => FindObjectsByNames<T>((IEnumerable<string>)names);

        /// <summary>Finds multi objects of type <typeparamref name="T"/> given their <paramref name="names"/></summary>
        /// <returns>An array of the first object found with each name</returns>
        /// <param name="names">the names of the objects to search for</param>
        public static T[] FindObjectsByNames<T>(IEnumerable<string> names) where T : Object
        {
            var ns = names.ToArray();
            var os = new T[ns.Length];
            var f = new bool[os.Length];
            foreach (var o in Resources.FindObjectsOfTypeAll<T>())
            {
                var c = 0;
                for (int i = 0; i < f.Length; i++)
                    if (f[i])
                        c++;
                    else if (ns[i] == o.name)
                    {
                        os[i] = o;
                        f[i] = true;
                        c++;
                    }
                if (c == f.Length)
                    break;
            }
            return os;
        }

        /// <summary>Finds an object of type <typeparamref name="T"/> given a <see cref="Predicate{T}"/></summary>
        /// <returns>The first object found</returns>
        /// <param name="predicate">the predicate to search for a match for</param>
        public static T FindObject<T>(Predicate<T> predicate) where T : Object => Resources.FindObjectsOfTypeAll<T>().FirstOrDefault((x) => predicate(x));

        /// <summary>Finds multi objects of type <typeparamref name="T"/> given a set of <see cref="Predicate{T}"/>s</summary>
        /// <returns>An array of the first object matching each <see cref="Predicate{T}"/></returns>
        /// <param name="predicates">the predicate to search for a match for</param>
        public static T[] FindObjects<T>(params Predicate<T>[] predicates) where T : Object => FindObjects((IEnumerable<Predicate<T>>)predicates);

        /// <summary>Finds multi objects of type <typeparamref name="T"/> given a set of <see cref="Predicate{T}"/>s</summary>
        /// <returns>An array of the first object matching each <see cref="Predicate{T}"/></returns>
        /// <param name="predicates">the predicate to search for a match for</param>
        public static T[] FindObjects<T>(IEnumerable<Predicate<T>> predicates) where T : Object
        {
            var ps = predicates.ToArray();
            var os = new T[ps.Length];
            var f = new bool[os.Length];
            foreach (var o in Resources.FindObjectsOfTypeAll<T>())
            {
                var c = 0;
                for (int i = 0; i < f.Length; i++)
                    if (f[i])
                        c++;
                    else if (ps[i](o))
                    {
                        os[i] = o;
                        f[i] = true;
                        c++;
                    }
                if (c == f.Length)
                    break;
            }
            return os;
        }


        /// <summary>Creates a <see cref="SlimeAppearanceElement"/></summary>
        /// <returns>The created element</returns>
        /// <param name="Name">The name to give the <see cref="SlimeAppearanceElement"/> (this is not important)</param>
        /// <param name="appearanceObjects">The <see cref="SlimeAppearanceObject"/> prefabs to store in the <see cref="SlimeAppearanceElement"/></param>
        public static SlimeAppearanceElement CreateElement(string Name, params SlimeAppearanceObject[] appearanceObjects)
        {
            var e = ScriptableObject.CreateInstance<SlimeAppearanceElement>();
            e.name = Name;
            e.Name = Name;
            e.Prefabs = appearanceObjects;
            return e;
        }

        public static SlimeAppearanceElement Clone(this SlimeAppearanceElement element, string name = null)
        {
            var e = ScriptableObject.CreateInstance<SlimeAppearanceElement>();
            e.name = name ?? element.name;
            e.Name = name ?? element.Name;
            e.Prefabs = new SlimeAppearanceObject[element.Prefabs.Length];
            element.Prefabs.CopyTo(e.Prefabs, 0);
            return e;
        }

        /// <returns>The <see cref="SlimeAppearance"/> of a slime given an <see cref="SlimeAppearance.AppearanceSaveSet"/></returns>
        public static SlimeAppearance GetAppearance(this Identifiable.Id id, SlimeAppearance.AppearanceSaveSet saveSet) => SceneContext.Instance.SlimeAppearanceDirector.SlimeDefinitions.GetSlimeByIdentifiableId(id).GetAppearanceForSet(saveSet);

        /// <returns>The prefab of the <see cref="Identifiable.Id"/> from the <see cref="LookupDirector"/></returns>
        public static GameObject GetPrefab(this Identifiable.Id id) => GameContext.Instance.LookupDirector.GetPrefab(id);

        /// <summary>Attepts to get a <see cref="Component"/> of type <typeparamref name="T"/>. If the <see cref="Component"/> was not present on the <see cref="GameObject"/> it is added.</summary>
        /// <returns>An instance of <typeparamref name="T"/></returns>
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component => obj.GetComponent<T>() ?? obj.AddComponent<T>();

        /// <summary>Attepts to get a <see cref="Component"/> of type <typeparamref name="T"/>. If the <see cref="Component"/> was not present on the <see cref="GameObject"/> it is added.</summary>
        /// <returns>An instance of <typeparamref name="T"/></returns>
        public static T GetOrAddComponent<T>(this Component obj) where T : Component => obj.GetComponent<T>() ?? obj.gameObject.AddComponent<T>();

        /// <returns>A prefab duplicate of the object</returns>
        public static T CreatePrefab<T>(this T obj) where T : Object => Object.Instantiate(obj, Main.prefabParent, false);

        /// <returns>An inactive duplicate of the object</returns>
        public static T CreateInactive<T>(this T obj) where T : Component
        {
            var o = Object.Instantiate(obj, Main.prefabParent, true);
            o.gameObject.SetActive(false);
            o.transform.SetParent(null, true);

            return o;
        }

        /// <returns>A new <see cref="GameObject"/> that is a prefab</returns>
        public static GameObject CreateEmptyPrefab(string name)
        {
            var o = new GameObject(name);
            o.transform.SetParent(Main.prefabParent, false);
            return o;
        }

        /// <returns>Instantiates an inactive duplicate of the object</returns>
        public static GameObject CreateInactive(this GameObject obj)
        {
            var o = Object.Instantiate(obj, Main.prefabParent, true);
            o.SetActive(false);
            o.transform.SetParent(null, true);
            return o;
        }

        /// <summary>Useful for one-liner <see cref="ScriptableObject"/> creation.</summary>
        /// <typeparam name="T">the class to create an instance of</typeparam>
        /// <param name="construct">after creation, the created object will be given to this function. Use this to set variables on the object</param>
        /// <returns>The created object</returns>
        public static T CreateScriptableObject<T>(Action<T> construct = null) where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            construct?.Invoke(o);
            return o;
        }

        /// <returns>Returns all the children of <paramref name="parent"/>. If <paramref name="includeInactive"/> is false, inactive children are excluded</returns>
        public static List<Transform> GetAllChildren(this Transform parent, bool includeInactive = true)
        {
            var l = new List<Transform>();
            foreach (Transform t in parent)
                if (includeInactive || t.gameObject.activeSelf)
                    l.Add(t);
            return l;
        }

        /// <summary>
        /// Lighting options for the object rendering methods:<br/>
        /// <see cref="RenderImage(GameObject, RenderConfig, out Exception, bool, LightingMode, int, float)"/><br/>
        /// <see cref="RenderImages(GameObject, RenderConfig[], out Exception[], bool, LightingMode, int, float)"/>
        /// </summary>
        public enum LightingMode
        {
            /// <summary>Make no changes to the light sources for renders</summary>
            DoNotChange,
            /// <summary>Only include light sources part of the object itself for renders</summary>
            IgnoreExternalLights,
            /// <summary>Don't include any light sources in the renders</summary>
            IgnoreAllLights
        }

        /// <summary>
        /// Image generation settings for the object rendering methods:<br/>
        /// <see cref="RenderImage(GameObject, RenderConfig, out Exception, bool, LightingMode, int, float)"/><br/>
        /// <see cref="RenderImages(GameObject, RenderConfig[], out Exception[], bool, LightingMode, int, float)"/>
        /// </summary>
        public class RenderConfig
        {
            /// <summary>Action to be run before starting this render</summary>
            public Action<GameObject> BeforeRender;
            /// <summary>Action to be run after this render. Use for undoing changes made in the before action</summary>
            public Action<GameObject> AfterRender;
            /// <summary>Ambient lighting to use for this render. Leave <see langword="null"/> to use current</summary>
            public Color? ambientLight;
            /// <summary>Background color to use for the render. Transparent colors can be used</summary>
            public Color backgroundColor = Color.clear;
            /// <summary>The angle the camera should point during the render. Object will be centered in the image regardless</summary>
            public Quaternion cameraAngle;
            /// <summary>The margins of transparent pixels to ensure are around the rendered image. Set to <see langword="null"/> to skip margin checks</summary>
            public (uint left, uint bottom, uint right, uint top)? imageMargins = (0,0,0,0);
            /// <summary>Width in pixels of the image to be created</summary>
            public uint imgWidth;
            /// <summary>Height in pixels of the image to be created</summary>
            public uint imgHeight;
            /// <summary>Mipmap count of the image to be created</summary>
            public int mipmapCount = 0;
            /// <summary>Whether to use the linear color space or the sRGB color space for the image to be created</summary>
            public bool linearImg = false;
            /// <summary>Not recommended unless you're using an object that is only particles or the particles you want to be included are not already included</summary>
            public bool respectParticleRendererBounds = false;
            /// <summary>Whether to use a more detailed transparency detection. This will do more processing but will help if you're using a transparent background and are having issues with gamma artifacting in the render. This will have no effect if you're not using margins</summary>
            public bool complexTransparencyDetection = false;
            public RenderConfig(uint width,uint height,Quaternion angle)
            {
                imgWidth = width;
                imgHeight = height;
                cameraAngle = angle;
            }
        }

        static Texture2D RenderImage(GameObject go, RenderConfig renderConfig, out Exception exception, bool copyObjectForRender, LightingMode lightingMode, int renderLayer) => go.RenderImage(renderConfig, out exception, copyObjectForRender, lightingMode, renderLayer); // Preventing MissingMethodException
        /// <summary>
        /// Renders an image of the object using the provided configuration.
        /// </summary>
        /// <param name="copyObjectForRender">if <see langword="true"/>, it will instantiate the object and render the instantiated copy, otherwise will render the object itself. This will almost always need to be enabled for rendering a prefab</param>
        /// <param name="exception">Will be set to whatever <see cref="Exception"/> occured while trying to render, if one occured. Otherwise will be <see langword="null"/></param>
        /// <param name="go">The object to render an image of</param>
        /// <param name="lightingMode">Which light sources should be included in the render</param>
        /// <param name="renderConfig">The configuration to use for rendering the image</param>
        /// <param name="renderLayer">The layer to use for the render. It is recommended to leave as 30 to avoid unwanted object being included in the image</param>
        /// <param name="cameraBufferDistance">The amount of unrendered distance between the camera and the object. You shouldn't need to increase this unless you're getting strange object artifacting in the render</param>
        /// <returns><see langword="null"/> and sets the <paramref name="exception"/> value if an <see cref="Exception"/> occured, otherwise the generated image</returns>
        public static Texture2D RenderImage(this GameObject go, RenderConfig renderConfig, out Exception exception, bool copyObjectForRender = true, LightingMode lightingMode = LightingMode.DoNotChange, int renderLayer = 30, float cameraBufferDistance = 10)
        {
            var r = go.RenderImages(new[] { renderConfig },out var exceptions, copyObjectForRender,lightingMode,renderLayer,cameraBufferDistance)[0];
            exception = exceptions[0];
            return r;
        }
        static Texture2D[] RenderImages(GameObject go, RenderConfig[] renderConfigs, out Exception[] exceptions, bool copyObjectForRender, LightingMode lightingMode, int renderLayer) => go.RenderImages(renderConfigs, out exceptions, copyObjectForRender, lightingMode, renderLayer); // Preventing MissingMethodException
        /// <summary>
        /// Renders multiple images of the object using the provided configurations.
        /// </summary>
        /// <param name="copyObjectForRender">if <see langword="true"/>, it will instantiate the object and render the instantiated copy, otherwise will render the object itself. This will almost always need to be enabled for rendering a prefab</param>
        /// <param name="exceptions">Will be set to an array of the same size as <paramref name="renderConfigs"/>. For any renders that fail, the error that occured will be stored at the respective index</param>
        /// <param name="go">The object to render an image of</param>
        /// <param name="lightingMode">Which light sources should be included in the render</param>
        /// <param name="renderConfigs">The configurations to use for rendering the images</param>
        /// <param name="renderLayer">The layer to use for the renders. It is recommended to leave as 30 to avoid unwanted object being included in the images</param>
        /// <param name="cameraBufferDistance">The amount of unrendered distance between the camera and the object. You shouldn't need to increase this unless you're getting strange object artifacting in the renders</param>
        /// <returns>An array of the same size as <paramref name="renderConfigs"/> containing the images generated, respective of the indecies. If an error occured while generating a particular image, a <see langword="null"/> will be stored at that index and the exception will be stored in <paramref name="exceptions"/> at said index</returns>
        public static Texture2D[] RenderImages(this GameObject go, RenderConfig[] renderConfigs, out Exception[] exceptions, bool copyObjectForRender = true, LightingMode lightingMode = LightingMode.DoNotChange, int renderLayer = 30, float cameraBufferDistance = 10)
        {
            var results = new Texture2D[renderConfigs.Length];
            exceptions = new Exception[renderConfigs.Length];
            if (renderConfigs.Length == 0)
                return results;
            var created = new List<Object>();
            var originalTimeScale = Time.timeScale;
            var originalFixedTimeScale = Time.fixedDeltaTime;
            Time.timeScale = 0;
            Time.fixedDeltaTime = 0;
            var originalLights = new List<Light>();
            var originalSkybox = RenderSettings.skybox;
            var originalLight = RenderSettings.ambientLight;
            var originalFog = RenderSettings.fog;
            RenderSettings.fog = false;
            var originalLayers = new Dictionary<GameObject, int>();
            var originalForceRenderingOff = new HashSet<Renderer>();
            try
            {
                var o = go;
                var keepLights = new HashSet<Light>();
                if (copyObjectForRender)
                {
                    o = Object.Instantiate(o);
                    created.Add(o);
                } else
                    foreach (var t in o.GetComponentsInChildren<Transform>(true))
                        originalLayers[t.gameObject] = t.gameObject.layer;
                if (lightingMode == LightingMode.IgnoreExternalLights)
                    foreach (var l in o.GetComponentsInChildren<Light>())
                        keepLights.Add(l);
                if (lightingMode >= LightingMode.IgnoreExternalLights)
                {
                    foreach (var l in Object.FindObjectsOfType<Light>())
                        if (!keepLights.Contains(l) && l.enabled)
                        {
                            originalLights.Add(l);
                            l.enabled = false;
                        }
                    RenderSettings.skybox = null;
                }
                var c = new GameObject("").AddComponent<Camera>();
                created.Add(c.gameObject);
                c.orthographic = true;
                c.cullingMask = 1 << renderLayer;
                c.clearFlags = CameraClearFlags.SolidColor;
                c.nearClipPlane = cameraBufferDistance;
                c.useOcclusionCulling = false;
                c.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
                var ind = 0;
                foreach (var config in renderConfigs)
                {
                    try
                    {
                        config.BeforeRender?.Invoke(o);
                        if (config.imgHeight == 0 || config.imgWidth == 0)
                            throw new InvalidOperationException($"Image is too small [Width={config.imgWidth},Height={config.imgHeight}]");
                        var innerWidth = (int)(config.imageMargins != null ? config.imgWidth - config.imageMargins.Value.right - config.imageMargins.Value.left : config.imgWidth);
                        var innerHeight = (int)(config.imageMargins != null ? config.imgHeight - config.imageMargins.Value.top - config.imageMargins.Value.bottom : config.imgHeight);
                        if (innerHeight <= 0 || innerWidth <= 0)
                            throw new InvalidOperationException($"Image is too small [Width={config.imgWidth},Height={config.imgHeight},WidthWithoutMargins={innerWidth},HeightWithoutMargins={innerHeight}]");
                        var min = Vector3.positiveInfinity;
                        var max = Vector3.negativeInfinity;
                        var count = 0;
                        foreach (var rend in o.GetComponentsInChildren<Renderer>())
                        {
                            if (!originalForceRenderingOff.Contains(rend) && rend.forceRenderingOff)
                            {
                                rend.forceRenderingOff = false;
                                rend.enabled = true;
                                originalForceRenderingOff.Add(rend);
                            }
                            rend.gameObject.layer = renderLayer;
                            if (config.respectParticleRendererBounds || !(rend is ParticleSystemRenderer))
                            {
                                count++;
                                var b = rend.bounds;
                                var v = b.min;
                                min.x = Math.Min(min.x, v.x);
                                min.y = Math.Min(min.y, v.y);
                                min.z = Math.Min(min.z, v.z);
                                v = b.max;
                                max.x = Math.Max(max.x, v.x);
                                max.y = Math.Max(max.y, v.y);
                                max.z = Math.Max(max.z, v.z);
                            }
                        }
                        if (count == 0)
                            throw new InvalidOperationException("No bounds found on object");
                        var offs = new[] { max / 2 - min / 2, min / 2 - max / 2, default, default, default, default, default, default };
                        offs[2] = new Vector3(offs[0].x, offs[0].y, offs[1].z);
                        offs[3] = new Vector3(offs[0].x, offs[1].y, offs[0].z);
                        offs[4] = new Vector3(offs[0].x, offs[1].y, offs[1].z);
                        offs[5] = new Vector3(offs[1].x, offs[0].y, offs[0].z);
                        offs[6] = new Vector3(offs[1].x, offs[0].y, offs[1].z);
                        offs[7] = new Vector3(offs[1].x, offs[1].y, offs[0].z);
                        var size = Vector3.zero;
                        foreach (var i in offs)
                        {
                            var v = Quaternion.Inverse(config.cameraAngle) * i * 2;
                            size.x = Math.Max(size.x, Math.Abs(v.x));
                            size.y = Math.Max(size.y, Math.Abs(v.y));
                            size.z = Math.Max(size.z, Math.Abs(v.z));
                        }
                        c.aspect = (float)innerWidth / innerHeight;
                        var s = (float)Math.Ceiling(Math.Max(size.x / c.aspect, size.y));
                        if (float.IsNaN(s) || float.IsInfinity(s))
                            throw new InvalidOperationException("Failed to detect valid object bounds");
                        RenderSettings.ambientLight = config.ambientLight ?? originalLight;
                        var dir = config.cameraAngle * Vector3.forward;
                        c.transform.position = ((max + min) / 2) + (-dir * (s * 2 + cameraBufferDistance));
                        c.transform.rotation = config.cameraAngle;
                        c.orthographicSize = s / 2;
                        c.farClipPlane = s * 8 + cameraBufferDistance;
                        var r = RenderTexture.GetTemporary(innerWidth, innerHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                        c.pixelRect = new Rect(0, 0, r.width * 4, r.height * 4);
                        created.Add(r);
                        c.targetTexture = r;
                        var texture = new Texture2D((int)config.imgWidth, (int)config.imgHeight, TextureFormat.ARGB32, config.mipmapCount, config.linearImg);
                        if (config.imageMargins != null && config.imageMargins != (0, 0, 0, 0))
                        {
                            var colors = new Color[texture.width * texture.height];
                            for (int i = 0; i < colors.Length; i++)
                                colors[i] = config.backgroundColor;
                        }
                        var prev = RenderTexture.active;
                        RenderTexture.active = r;
                        if (config.imageMargins == null || !(config.complexTransparencyDetection && config.backgroundColor.a < 1))
                        {
                            c.backgroundColor = config.backgroundColor;
                            c.Render();
                            texture.ReadPixels(new Rect(0, 0, r.width, r.height), (int)(config.imageMargins?.left ?? 0), (int)(config.imageMargins?.bottom ?? 0));
                        }
                        else
                        {
                            c.backgroundColor = Color.red;
                            c.Render();
                            texture.ReadPixels(new Rect(0, 0, r.width, r.height), (int)(config.imageMargins?.left ?? 0), (int)(config.imageMargins?.bottom ?? 0));
                            var red = texture.GetPixels(0);
                            c.backgroundColor = Color.green;
                            c.Render();
                            texture.ReadPixels(new Rect(0, 0, r.width, r.height), (int)(config.imageMargins?.left ?? 0), (int)(config.imageMargins?.bottom ?? 0));
                            var green = texture.GetPixels(0);
                            c.backgroundColor = Color.blue;
                            c.Render();
                            texture.ReadPixels(new Rect(0, 0, r.width, r.height), (int)(config.imageMargins?.left ?? 0), (int)(config.imageMargins?.bottom ?? 0));
                            var blue = texture.GetPixels(0);
                            for (int i = 0; i < red.Length; i++)
                                red[i] = GetCommon(red[i], green[i], blue[i]);
                            if (config.imageMargins == null && config.backgroundColor.a > 0)
                                for (int i = 0; i < red.Length; i++)
                                    red[i] = config.backgroundColor.Overlay(red[i]);
                            texture.SetPixels(red, 0);
                        }
                        if (config.imageMargins != null)
                        {
                            var edge = texture.FindEdges(config.complexTransparencyDetection ? Color.clear : config.backgroundColor, (int)config.imageMargins.Value.left, (int)config.imageMargins.Value.bottom, (int)config.imageMargins.Value.right, (int)config.imageMargins.Value.top);
                            if ((edge.minX > config.imageMargins.Value.left || edge.maxX < config.imgWidth - config.imageMargins.Value.right - 1) && (edge.minY > config.imageMargins.Value.bottom || edge.maxY < config.imgHeight - config.imageMargins.Value.top - 1))
                            {
                                var scale = Math.Max((edge.maxX - edge.minX + 1) / (float)r.width, (edge.maxY - edge.minY + 1) / (float)r.height);
                                var off = new Vector2((edge.maxX - edge.minX + 1 - r.width) / 2f, r.height / 2);
                                c.transform.position += c.transform.up * ((edge.maxY + edge.minY - r.height) / 2f / r.height * s) + c.transform.right * ((edge.maxX + edge.minX - r.width) / 2f / r.height * s);
                                c.orthographicSize *= scale;
                                if (config.complexTransparencyDetection && config.backgroundColor.a < 1)
                                {
                                    c.backgroundColor = Color.red;
                                    c.Render();
                                    texture.ReadPixels(new Rect(0, 0, r.width, r.height), (int)(config.imageMargins?.left ?? 0), (int)(config.imageMargins?.bottom ?? 0));
                                    var red = texture.GetPixels(0);
                                    c.backgroundColor = Color.green;
                                    c.Render();
                                    texture.ReadPixels(new Rect(0, 0, r.width, r.height), (int)(config.imageMargins?.left ?? 0), (int)(config.imageMargins?.bottom ?? 0));
                                    var green = texture.GetPixels(0);
                                    c.backgroundColor = Color.blue;
                                    c.Render();
                                    texture.ReadPixels(new Rect(0, 0, r.width, r.height), (int)(config.imageMargins?.left ?? 0), (int)(config.imageMargins?.bottom ?? 0));
                                    var blue = texture.GetPixels(0);
                                    for (int i = 0; i < red.Length; i++)
                                        red[i] = GetCommon(red[i], green[i], blue[i]);
                                    if (config.backgroundColor.a > 0)
                                        for (int i = 0; i < red.Length; i++)
                                            red[i] = config.backgroundColor.Overlay(red[i]);
                                    texture.SetPixels(red, 0);
                                }
                                else
                                {
                                    c.backgroundColor = config.backgroundColor;
                                    c.Render();
                                    texture.ReadPixels(new Rect(0, 0, r.width, r.height), (int)(config.imageMargins?.left ?? 0), (int)(config.imageMargins?.bottom ?? 0));
                                }
                            }
                        }
                        texture.Apply();
                        RenderTexture.active = prev;
                        RenderTexture.ReleaseTemporary(r);
                        results[ind] = texture;
                    }
                    catch (Exception e)
                    {
                        exceptions[ind] = e;
                        results[ind] = null;
                    }
                    try { config.AfterRender?.Invoke(o); } catch { }
                    ind++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                foreach (var i in created)
                    if (i)
                    {
                        if (i is RenderTexture r)
                            RenderTexture.ReleaseTemporary(r);
                        else
                            Object.DestroyImmediate(i);
                    }
                Time.timeScale = originalTimeScale;
                Time.fixedDeltaTime = originalFixedTimeScale;
                RenderSettings.skybox = originalSkybox;
                RenderSettings.ambientLight = originalLight;
                RenderSettings.fog = originalFog;
                foreach (var l in originalLights)
                    l.enabled = true;
                foreach (var p in originalLayers)
                    if (p.Key)
                        p.Key.layer = p.Value;
                foreach (var r in originalForceRenderingOff)
                    if (r)
                        r.forceRenderingOff = true;
            }
            return results;
        }

        static Color GetCommon(Color r, Color g, Color b)
        {
            if (r == g)
                return r;
            if (r == Color.red && g == Color.green)
                return Color.clear;
            var vr = Math.Max(g.r, b.r);
            var vg = Math.Max(r.g, b.g);
            var vb = Math.Max(r.b, g.b);
            var a = Math.Max(Math.Max(1 - g.g + vg, 1 - r.r + vr), 1 - b.b + vb);
            return new Color(vr / a, vg / a, vb / a, a);
        }
    }

    public static class TextUtils
    {
        /// <summary>
        /// <para>Loads a text file from the embedded resources.</para>
        /// <para>Throws a <see cref="MissingResourceException"/> if no file is found under the specified name.</para>
        /// </summary>
        /// <param name="filename">the name of the file to load (including extention)</param>
        /// <param name="encoding">The character encoding to read the file with</param>
        /// <returns>The contents of the file</returns>
        /// <exception cref="MissingResourceException"/>
        public static string LoadText(string filename, System.Text.Encoding encoding = default)
        {
            if (encoding == default)
                encoding = System.Text.Encoding.Default;
            var modAssembly = Assembly.GetCallingAssembly();
            filename = modAssembly.GetName().Name + "." + filename;
            var stream = modAssembly.GetManifestResourceStream(filename);
            if (stream == null)
                throw new MissingResourceException(filename);
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return encoding.GetString(bytes);
        }
        /// <summary>
        /// <para>Loads a json file from the embedded resources.</para>
        /// <para>Throws a <see cref="MissingResourceException"/> if no file is found under the specified name.</para>
        /// </summary>
        /// <param name="filename">the name of the file to load (including extention)</param>
        /// <returns>The <see cref="JsonValue"/> generated from the file's contents. In most cases this will be a <see cref="JsonObject"/></returns>
        /// <exception cref="MissingResourceException"/>
        public static JsonValue LoadJson(string filename)
        {
            var modAssembly = Assembly.GetCallingAssembly();
            filename = modAssembly.GetName().Name + "." + filename;
            var stream = modAssembly.GetManifestResourceStream(filename);
            if (stream == null)
                throw new MissingResourceException(filename);
            return JsonValue.Load(stream);
        }
        /// <summary>Creates a <see cref="JsonValue"/> from a raw json <see cref="string"/></summary>
        /// <param name="rawJson">the <see cref="string"/> to create the <see cref="JsonValue"/> from</param>
        /// <returns>The <see cref="JsonValue"/> generated from <paramref name="rawJson"/>. In most cases this will be a <see cref="JsonObject"/></returns>
        public static JsonValue LoadRawJson(string rawJson) => JsonValue.Parse(rawJson);
    }

    public static class DroneNetworkUtils
    {
        /// <summary>
        /// <para>Creates a set of <see cref="PathingNetworkNode"/>s as depicted by the provided <see cref="JsonObject"/> and adds them<br/>
        /// to the provided cell. During this process, if the provided cell does not already contain a<br/>
        /// <see cref="DroneNetwork"/> component, one will be added.</para>
        /// <para>If the cell already has a <see cref="DroneNetwork"/>, existing nodes will not be removed</para>
        /// <para>Throws a <see cref="FormatException"/> if one of the json elements is the wrong type.<br/>
        /// Throws a <see cref="MissingMemberException"/> if one of the json elements is missing a required property.</para>
        /// </summary>
        /// <param name="cell">The <see cref="CellDirector"/> of the cell to add the nodes to</param>
        /// <param name="json">The JSON to generate the nodes from</param>
        /// <param name="automatic2WayConnections">If <see langword="true"/>, all the node connections created will have a second reverse connection.<br/><see cref="Drone"/>s may not path as expected if connections are only one way</param>
        /// <returns>The longest created connection length. If the distance between nodes is too great, drones may<br/>be unable to use the connection</returns>
        /// <exception cref="FormatException"/>
        /// <exception cref="MissingMemberException"/>
        public static float CreateNodesFromJson(CellDirector cell, JsonObject json, bool automatic2WayConnections = true)
        {
            var flag = false;
            var network = cell.GetComponent<DroneNetwork>();
            if (!network)
            {
                flag = true;
                if (cell.gameObject.activeSelf)
                    Patch_PathingNetwork.prevent = true;
                network = cell.gameObject.AddComponent<DroneNetwork>();
                Patch_PathingNetwork.prevent = false;
            }
            if (network.nodesParent == null)
            {
                network.nodesParent = new GameObject("droneNodeParent");
                network.nodesParent.transform.SetParent(cell.GetComponent<Region>().root.transform, false);
            }
            var parent = network.nodesParent.transform;
            var pather = network.Pather;
            var nodes = pather.nodes.ToList();
            var nodeMem = new Dictionary<PathingNetworkNode, List<object>>();
            try
            {
                foreach (var p in json)
                {
                    if (p.Value.JsonType != JsonType.Object)
                        throw new FormatException($"Element {p.Key} is {p.Value.JsonType}. Expected: {JsonType.Object}");
                    if (!p.Value.ContainsKey("position"))
                        throw new MissingMemberException($"Element {p.Key} is missing the \"position\" property");
                    var posJson = p.Value["position"];
                    if (posJson.JsonType != JsonType.Array)
                        throw new FormatException($"Element {p.Key}.position is {posJson.JsonType}. Expected: {JsonType.Array}");
                    if (posJson.Count != 3)
                        throw new FormatException($"Element {p.Key}.position has length {posJson.Count}. Expected: 3");
                    for (int i = 0; i < 3; i++)
                        if (posJson[i].JsonType != JsonType.Number)
                            throw new FormatException($"Element {p.Key}.position[{i}] is {posJson[i].JsonType}. Expected: {JsonType.Number}");
                    var pos = new Vector3(posJson[0], posJson[1], posJson[2]);
                    var mem = new List<object>();
                    if (!p.Value.ContainsKey("connections"))
                        throw new MissingMemberException($"Element {p.Key} is missing the \"connections\" property");
                    var connectJson = p.Value["connections"];
                    if (connectJson.JsonType != JsonType.Array)
                        throw new FormatException($"Element {p.Key}.connections is {connectJson.JsonType}. Expected: {JsonType.Array}");
                    int j = 0;
                    foreach (var c in (JsonArray)connectJson)
                    {
                        if (c.JsonType == JsonType.Number)
                            mem.Add((int)c);
                        else if (c.JsonType == JsonType.String)
                            mem.Add((string)c);
                        else
                            throw new FormatException($"Element {p.Key}.connections[{j}] is {connectJson.JsonType}. Expected: {JsonType.Number} or {JsonType.String}");
                        j++;
                    }
                    var n = new GameObject(p.Key).AddComponent<PathingNetworkNode>();
                    nodes.Add(n);
                    n.transform.SetParent(parent, false);
                    n.transform.position = pos;
                    nodeMem.Add(n, mem);
                    n.nodeLoc = n.transform;
                }
            }
            catch (Exception e)
            {
                foreach (var n in nodeMem.Keys)
                    Object.DestroyImmediate(n.gameObject);
                if (flag)
                {
                    Patch_PathingNetwork.prevent = false;
                    Object.DestroyImmediate(network);
                }
                throw e;
            }
            pather.nodes = nodes.ToArray();
            float max = 0;
            foreach (var p in nodeMem)
                foreach (var o in p.Value)
                    if (o is string)
                    {
                        var f = nodes.FirstOrDefault((x) => x.name == (string)o);
                        if (f == null)
                            Debug.LogWarning("Node " + o + " was not found. Connection not created");
                        else
                        {
                            if (p.Key.connections == null)
                                p.Key.connections = new List<PathingNetworkNode>();
                            if (!p.Key.connections.Contains(f))
                                p.Key.connections.Add(f);
                            if (automatic2WayConnections)
                            {
                                if (f.connections == null)
                                    f.connections = new List<PathingNetworkNode>();
                                if (!f.connections.Contains(p.Key))
                                    f.connections.Add(p.Key);
                            }
                            max = Mathf.Max(max, (p.Key.position - f.position).sqrMagnitude);
                        }
                    }
                    else
                    {
                        if ((int)o < 0)
                            Debug.LogWarning("Node index " + o + " was less than 0. Connection not created");
                        else if ((int)o >= nodes.Count)
                            Debug.LogWarning("Node index " + o + " was greater than " + (nodes.Count - 1) + ". Connection not created");
                        else
                        {
                            if (p.Key.connections == null)
                                p.Key.connections = new List<PathingNetworkNode>();
                            var f = nodes[(int)o];
                            if (!p.Key.connections.Contains(f))
                                p.Key.connections.Add(f);
                            if (automatic2WayConnections)
                            {
                                if (f.connections == null)
                                    f.connections = new List<PathingNetworkNode>();
                                if (!f.connections.Contains(p.Key))
                                    f.connections.Add(p.Key);
                            }
                            max = Mathf.Max(max, (p.Key.position - f.position).sqrMagnitude);
                        }
                    }
            return Mathf.Sqrt(max);
        }
    }

    public static class ExtentionMethods
    {

        /// <summary>Searches for an item matching the provided <see cref="Predicate{T}"/>. Starts searching at <paramref name="start"/>.<br/>
        /// If no matching value is found after <paramref name="start"/> then it will search from the first item up to <paramref name="start"/></summary>
        /// <returns>The index of the found item. If no item was found returns -1</returns>
        /// <param name="start">the index to start the search at</param>
        /// <param name="predicate">the condition to search for</param>
        public static int FindIndexLoop<T>(this List<T> s, int start, Predicate<T> predicate)
        {
            start = start.Mod(s.Count);
            var e = s.FindIndex(start, predicate);
            var e2 = -1;
            if (e == -1 || e == s.Count - 1)
                e2 = s.FindIndex(predicate);
            if (e2 != -1)
                return e2;
            return e;
        }

        /// <summary>
        /// <para>Gets the item from the list at index <paramref name="index"/>.</para>
        /// <para>If index is outside the bounds of the list then it will be wrapped to the list's<br/>
        /// length. For example, -1 will be the last item in the list</para>
        /// </summary>
        /// <returns>The item at the specified index</returns>
        /// <param name="index">the index of the item to fetch</param>
        public static T Get<T>(this List<T> s, int index) => s[index.Mod(s.Count)];

        /// <summary>
        /// <para>Gets the result of a non-negative modular division</para>
        /// </summary>
        public static int Mod(this int o, int v) => o % v + (o < 0 ? v : 0);


        /// <summary>
        /// <para>Attempts to find an item within a list. If the item is found its index is returned, otherwise<br/>
        /// the item is added to the list and the new item's index is returned</para>
        /// </summary>
        /// <returns>The index of the specified item</returns>
        /// <param name="value">the value to get the index of</param>
        public static int AddOrGetIndex<T>(this List<T> t, T value)
        {
            var i = t.IndexOf(value);
            if (i == -1)
            {
                i = t.Count;
                t.Add(value);
            }
            return i;
        }

        /// <summary>Recursively searchs all the children of the <see cref="Transform"/> to find all that have a certain name</summary>
        /// <returns>A list of the found children's <see cref="Transform"/>s</returns>
        /// <param name="ChildName">the name of the children to search for</param>
        public static List<Transform> FindChildrenRecursively(this Transform transform, string ChildName)
        {
            var t = new List<Transform>();
            transform.GetChildren_Internal(ChildName, t);
            return t;
        }

        static void GetChildren_Internal(this Transform transform, string ChildName, List<Transform> transforms)
        {
            if (transform.name == ChildName)
                transforms.Add(transform);
            foreach (Transform t in transform)
                t.GetChildren_Internal(ChildName, transforms);
        }

        /// <summary>Adds an id to the <see cref="SlimeEat.FoodGroup"/></summary>
        public static void AddItem(this SlimeEat.FoodGroup foodGroup, Identifiable.Id ident) => SlimeEat.foodGroupIds[foodGroup] = SlimeEat.foodGroupIds.TryGetValue(foodGroup, out var v) ? v.AddToArray(ident) : new Identifiable.Id[] { ident };

        /// <summary>Rotates a <see cref="Vector3"/> by the provided <see cref="Quaternion"/></summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Quaternion rotation) => rotation * value;
        /// <summary>Rotates a <see cref="Vector3"/> by the provided Euler</summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Vector3 rotation) => value.Rotate(Quaternion.Euler(rotation));
        /// <summary>Rotates a <see cref="Vector3"/> by the provided Euler</summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, float x, float y, float z) => value.Rotate(Quaternion.Euler(x, y, z));
        /// <summary>Rotates a <see cref="Vector3"/> by the provided <see cref="Quaternion"/> around the specified point</summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Quaternion rotation, Vector3 rotatePoint) => value.Offset(-rotatePoint).Rotate(rotation).Offset(rotatePoint);
        /// <summary>Rotates a <see cref="Vector3"/> by the provided Euler around the specified point</summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Vector3 rotation, Vector3 rotatePoint) => value.Rotate(Quaternion.Euler(rotation), rotatePoint);
        /// <summary>Rotates a <see cref="Vector3"/> by the provided Euler around the specified point</summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, float x, float y, float z, Vector3 rotatePoint) => value.Rotate(Quaternion.Euler(x, y, z), rotatePoint);
        /// <summary>Rotates a <see cref="Vector3"/> by the provided <see cref="Quaternion"/> around the specified point</summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Quaternion rotation, float rotatePointX, float rotatePointY, float rotatePointZ) => value.Rotate(rotation, new Vector3(rotatePointX, rotatePointY, rotatePointZ));
        /// <summary>Rotates a <see cref="Vector3"/> by the provided Euler around the specified point</summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Vector3 rotation, float rotatePointX, float rotatePointY, float rotatePointZ) => value.Rotate(Quaternion.Euler(rotation), new Vector3(rotatePointX, rotatePointY, rotatePointZ));
        /// <summary>Rotates a <see cref="Vector3"/> by the provided Euler around the specified point</summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, float x, float y, float z, float rotatePointX, float rotatePointY, float rotatePointZ) => value.Rotate(Quaternion.Euler(x, y, z), new Vector3(rotatePointX, rotatePointY, rotatePointZ));
        /// <summary>Offsets a <see cref="Vector3"/> by the provided <see cref="Vector3"/></summary>
        /// <returns>The offset <see cref="Vector3"/></returns>
        public static Vector3 Offset(this Vector3 value, float x, float y, float z) => value.Offset(new Vector3(x, y, z));
        /// <summary>Offsets a <see cref="Vector3"/> by the provided <see cref="Vector3"/></summary>
        /// <returns>The offset <see cref="Vector3"/></returns>
        public static Vector3 Offset(this Vector3 value, Vector3 offset) => value + offset;
        /// <summary>Multiplies a <see cref="Vector3"/> by the provided <see cref="Vector3"/></summary>
        /// <returns>The scaled <see cref="Vector3"/></returns>
        public static Vector3 Multiply(this Vector3 value, float x, float y, float z) => new Vector3(value.x * x, value.y * y, value.z * z);
        /// <summary>Multiplies a <see cref="Vector3"/> by the provided <see cref="Vector3"/></summary>
        /// <returns>The scaled <see cref="Vector3"/></returns>
        public static Vector3 Multiply(this Vector3 value, float scale) => value.Multiply(scale, scale, scale);
        /// <summary>Multiplies a <see cref="Vector3"/> by the provided <see cref="Vector3"/></summary>
        /// <returns>The scaled <see cref="Vector3"/></returns>
        public static Vector3 Multiply(this Vector3 value, Vector3 scale) => value.Multiply(scale.x, scale.y, scale.z);
        /// <summary>Multiplies a <see cref="Vector2"/> by the provided <see cref="Vector2"/></summary>
        /// <returns>The scaled <see cref="Vector2"/></returns>
        public static Vector2 Multiply(this Vector2 value, float x, float y) => new Vector2(value.x * x, value.y * y);
        /// <summary>Multiplies a <see cref="Vector2"/> by the provided <see cref="Vector2"/></summary>
        /// <returns>The scaled <see cref="Vector2"/></returns>
        public static Vector2 Multiply(this Vector2 value, Vector2 scale) => value.Multiply(scale.x, scale.y);
        /// <summary>Offsets a <see cref="Vector2"/> by the provided <see cref="Vector2"/></summary>
        /// <returns>The offset <see cref="Vector2"/></returns>
        public static Vector2 Offset(this Vector2 value, float x, float y) => value.Offset(new Vector2(x, y));
        /// <summary>Offsets a <see cref="Vector2"/> by the provided <see cref="Vector2"/></summary>
        /// <returns>The offset <see cref="Vector2"/></returns>
        public static Vector2 Offset(this Vector2 value, Vector2 offset) => value + offset;
        /// <summary>Rotates a <see cref="Vector2"/> by the provided angle in degrees</summary>
        /// <returns>The rotated <see cref="Vector2"/></returns>
        public static Vector2 Rotate(this Vector2 value, float angle) => Quaternion.Euler(0, 0, angle) * value;
        /// <summary>Rotates a <see cref="Vector2"/> by the provided angle in degrees around the provided point</summary>
        /// <returns>The rotated <see cref="Vector2"/></returns>
        public static Vector2 Rotate(this Vector2 value, float angle, Vector2 rotatePoint) => value.Offset(-rotatePoint).Rotate(angle).Offset(rotatePoint);
        /// <summary>Rotates a <see cref="Vector2"/> by the provided angle in degrees around the provided point</summary>
        /// <returns>The rotated <see cref="Vector2"/></returns>
        public static Vector2 Rotate(this Vector2 value, float angle, float x, float y) => value.Rotate(angle, new Vector2(x, y));
        /// <returns>The <see cref="Vector3"/>'s x, y and z values in an array</returns>
        public static float[] ToArray(this Vector3 value) => new float[] { value.x, value.y, value.z };
        /// <returns>The <see cref="Vector3"/>'s smallest value</returns>
        public static float Min(this Vector3 value) => Mathf.Min(value.ToArray());
        /// <returns>The <see cref="Vector3"/>'s biggest value</returns>
        public static float Max(this Vector3 value) => Mathf.Max(value.ToArray());
        /// <returns>The <see cref="Vector3"/> with all axes positive.</returns>
        public static Vector3 Abs(this Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

        /// <returns>A duplicate of the <see cref="Material"/>.</returns>
        public static Material Clone(this Material material)
        {
            var m = new Material(material);
            m.CopyPropertiesFromMaterial(material);
            return m;
        }


        /// <summary>Copies all the field's values from <paramref name="b"/>.</summary>
        public static void CopyFields<T>(this T a, T b)
        {
            var t = typeof(T);
            while (t != typeof(Object) && t != typeof(object))
            {
                foreach (var f in t.GetFields((BindingFlags)(-1)))
                    if (!f.IsStatic)
                        f.SetValue(a, f.GetValue(b));
                t = t.BaseType;
            }
        }

        /// <summary>Overlays the <see cref="Color"/> with <paramref name="color"/> with respect to alpha values</summary>
        public static Color Overlay(this Color c, Color color) => new Color(c.r * (1 - color.a) + color.r * color.a, c.g * (1 - color.a) + color.g * color.a, c.b * (1 - color.a) + color.b * color.a, Mathf.Max(c.a, color.a));

        public static Color Multiply(this Color color, float r, float g, float b) => new Color(color.r * r, color.g * g, color.b * b, color.a);
        public static Color Multiply(this Color color, float m) => new Color(color.r * m, color.g * m, color.b * m, color.a);

        /// <summary>Returns the <see cref="Color"/> grayscaled then tinted</summary>
        public static Color Shift(this Color color, float r, float g, float b)
        {
            var v = color.grayscale;
            return new Color(v * r, v * g, v * b, color.a);
        }

        /// <summary>Selects a random object from a collection given the weights fetched from the <paramref name="getWeight"/> function</summary>
        /// <returns>A random object</returns>
        public static T RandomObject<T>(this IEnumerable<T> c, Func<T, float> getWeight)
        {
            var r = Random.Range(0, c.Sum(getWeight));
            foreach (var i in c)
            {
                r -= getWeight(i);
                if (r < 0)
                    return i;
            }
            return default;
        }

        /// <summary>Adds an item to a list if the list does not already contain the item</summary>
        /// <param name="value">item to try and add to the list</param>
        /// <returns><see langword="true"/> if the item was added, <see langword="false"/> if the list already contains the item</returns>
        public static bool AddUnique<T>(this List<T> l, T value)
        {
            if (l.Contains(value))
                return false;
            l.Add(value);
            return true;
        }

        /// <summary>Performs an <see cref="AddUnique"/> for each item in the set <paramref name="values"/></summary>
        public static void AddRangeUnique<T>(this List<T> l, IEnumerable<T> values)
        {
            foreach (var v in values)
                l.AddUnique(v);
        }

        /// <summary>
        /// <para>Gets the values associated with the specified <paramref name="keys"/>.</para>
        /// <para>If a specific key is not found, <see langword="default"/> will be fetched as it's value</para>
        /// </summary>
        /// <param name="keys">keys to fetch</param>
        public static List<Y> GetValues<X, Y>(this Dictionary<X, Y> d, IEnumerable<X> keys)
        {
            var r = new List<Y>();
            foreach (var key in keys)
                if (d.TryGetValue(key, out var value))
                    r.Add(value);
                else
                    r.Add(default);
            return r;
        }

        /// <summary>
        /// <para>Adds the set of keys and values to the dictionary.</para>
        /// <para>If the keys and values collections are different sizes, only the less amount will be used. For example, if keys is 5 long and values is 10 long, only 5 items will be added to the dictionary</para>
        /// </summary>
        public static void AddRange<X, Y>(this Dictionary<X, Y> d, IEnumerable<X> keys, IEnumerable<Y> values)
        {
            var k = keys.GetEnumerator();
            var v = values.GetEnumerator();
            while (k.MoveNext() && v.MoveNext())
                d.Add(k.Current, v.Current);
        }

        /// <summary>Attempts to find an item in the collection. Once the item is found, it is converted to another object/value</summary>
        /// <param name="value">The first item that matches the <paramref name="predicate"/> and has been parsed through the <paramref name="getter"/>. If no match was found, it will be <see langword="default"/></param>
        /// <returns><see langword="true"/> if an item matching the <paramref name="predicate"/> was found, otherwise, <see langword="false"/></returns>
        public static bool TryFind<X, Y>(this IEnumerable<X> c, Predicate<X> predicate, Func<X, Y> getter, out Y value)
        {
            foreach (var i in c)
                if (predicate(i))
                {
                    value = getter(i);
                    return true;
                }
            value = default;
            return false;
        }

        /// <summary>Checks if type <paramref name="t"/> is the same as or inherits from type <typeparamref name="T"/></summary>
        public static bool Is<T>(this Type t) => typeof(T).IsAssignableFrom(t);

        public static List<T> GetAll<T>(this IEnumerable<T> c, Predicate<T> predicate, bool enforceUnique = false)
        {
            var l = new List<T>();
            foreach (var i in c)
                if (predicate(i))
                {
                    if (enforceUnique)
                        l.AddUnique(i);
                    else
                        l.Add(i);
                }
            return l;
        }
        public static List<Y> Cast<X,Y>(this IEnumerable<X> c, Func<X,Y> converter)
        {
            var l = new List<Y>();
            foreach (var i in c)
                l.Add(converter(i));
            return l;
        }
        public static List<Z> For<X,Y,Z>(this X o,Func<X,Y> start,Func<X,Y,bool> hasReachedEnd,Func<X,Y,Y> progress,Func<X,Y,Z> collector,Func<X,Y,bool> include = null)
        {
            var l = new List<Z>();
            for (var i = start(o); !hasReachedEnd(o, i); i = progress(o, i))
                if (include?.Invoke(o, i) ?? true)
                    l.Add(collector(o, i));
            return l;
        }
        internal static (int minX, int minY, int maxX, int maxY) FindEdges(this Texture2D texture, Color background, int startX = 0, int startY = 0, int endXOffset = 0, int endYOffset = 0)
        {
            var x1 = texture.width - 1;
            var y1 = texture.height - 1;
            var x2 = 0;
            var y2 = 0;
            for (int x = startX; x < texture.width - endXOffset; x++)
                for (int y = startY; y < texture.height - endYOffset; y++)
                    if (background.a == 0 ? texture.GetPixel(x, y).a != 0 : texture.GetPixel(x, y) != background)
                    {
                        x1 = Math.Min(x1, x);
                        x2 = Math.Max(x2, x);
                        y1 = Math.Min(y1, y);
                        y2 = Math.Max(y2, y);
                    }
            return (x1, y1, x2, y2);
        }
    }

    /// <summary>A set of <see cref="Color"/>s that behaves similar to a spectrum or gradient</summary>
    public class ColorGroup
    {
        List<Color> colors = new List<Color>();
        List<float> positions = new List<float>();

        /// <summary>Adds a <see cref="Color"/> to the <see cref="ColorGroup"/> at the specified position</summary>
        public void AddColor(Color color, float position)
        {
            position = Mathf.Clamp01(position);
            colors.Add(color);
            positions.Add(position);
            var c = colors.ToArray();
            var p = positions.ToArray();
            System.Array.Sort(p, c);
            colors = c.ToList();
            positions = p.ToList();
        }
        /// <summary>
        /// <para>Gets a <see cref="Color"/> at the specified position.</para>
        /// <para>In the case of a <paramref name="position"/> between added colors it will return a <see cref="Color"/> partway between them</para>
        /// </summary>
        public Color GetColor(float position)
        {
            position = Mathf.Clamp01(position);
            if (colors.Count == 0)
                return Color.clear;
            if (colors.Count == 1)
                return colors[0];
            var i = positions.FindIndex((x) => x > position);
            if (i == -1)
                return colors[colors.Count - 1];
            if (i == 0)
                return colors[0];
            var p = (position - positions[i - 1]) / (positions[i] - positions[i - 1]);
            var c = colors[i] * p + colors[i - 1] * (1 - p);
            return c;
        }
    }

    /// <summary>A basic animation handler class</summary>
    public abstract class TransformAnimator<T> : MonoBehaviour
    {
        float current = 0;
        public float AnimationTime = 1;
        T start;
        T target;
        protected T Start => start;
        protected T Target => target;
        protected abstract void UpdateProgress(float progress);
        void Update()
        {
            if (current < AnimationTime)
            {
                current += Time.deltaTime;
                if (current >= AnimationTime)
                    current = AnimationTime;
                UpdateProgress(current / AnimationTime);
            }
        }
        protected void EndAnimation()
        {
            current = AnimationTime;
            UpdateProgress(1);
        }
        protected void ResetAnimation()
        {
            current = 0;
            UpdateProgress(0);
        }
        public void SetTarget(T Target)
        {
            if (target.Equals(Target))
                return;
            start = GetStart();
            target = Target;
            ResetAnimation();
        }
        public void SetImmediate(T Target)
        {
            target = Target;
            EndAnimation();
        }
        protected abstract T GetStart();
    }

    /// <summary>Used for animating the <see cref="Transform.localScale"/> property</summary>
    public class ScaleAnimator : TransformAnimator<Vector3>
    {
        protected override Vector3 GetStart() => transform.localScale;
        protected override void UpdateProgress(float progress) => transform.localScale = Start + (Target - Start) * progress;
    }

    /// <summary>Used for animating the <see cref="RectTransform.offsetMin"/> and <see cref="RectTransform.offsetMax"/> properties</summary>
    public class OffsetAnimator : TransformAnimator<Vector4>
    {
        protected override Vector4 GetStart()
        {
            var rect = transform as RectTransform;
            return new Vector4(rect.offsetMin.x, rect.offsetMin.y, rect.offsetMax.x, rect.offsetMax.y);
        }
        protected override void UpdateProgress(float progress)
        {
            var p = Start + (Target - Start) * progress;
            var rect = transform as RectTransform;
            rect.offsetMin = new Vector2(p.x, p.y);
            rect.offsetMax = new Vector2(p.z, p.w);
        }
    }

    /// <summary>Used for animating the <see cref="Transform.localRotation"/> in the form of Z axis rotations</summary>
    public class RotationAnimator : TransformAnimator<float>
    {
        protected override float GetStart() => transform.localRotation.eulerAngles.z;
        protected override void UpdateProgress(float progress) => transform.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, Start), Quaternion.Euler(0, 0, Target), progress);
    }

    public class ModeOption : DroneMetadata.Program.BaseComponent
    {
        Action onClick;
        public ModeOption(Sprite Icon, string NameKey, Action OnSelect)
        {
            id = NameKey;
            image = Icon;
            onClick = OnSelect;
        }
        public void Selected() => onClick?.Invoke();
    }

    public class AdaptiveModeOption : ModeOption
    {
        Func<Sprite> getIcon;
        Func<string> getName;
        public AdaptiveModeOption(Func<Sprite> Icon, Func<string> Name, Action OnSelect) : base(null, null, OnSelect)
        {
            getName = Name;
            getIcon = Icon;
        }
        public override Sprite GetImage() => getIcon();
        public override string GetName() => getName();
    }

    /// <summary>A basic mesh data handling class. Can be used for easy <see cref="Mesh"/> combining and editing</summary>
    public struct MeshData
    {
        List<Vector3> _v;
        List<Vector2> _u;
        List<int> _t;
        public Vector3[] vertices => _v.ToArray();
        public Vector2[] uvs => _u.ToArray();
        public int[] triangles => _t.ToArray();
        /// <returns>An empty set of <see cref="MeshData"/></returns>
        public static MeshData Empty => new MeshData(new Vector3[0], new Vector2[0], new int[0]);
        /// <param name="vertices">Must be the same length as <paramref name="uvs"/></param>
        /// <param name="uvs">Must be the same length as <paramref name="vertices"/></param>
        /// <param name="triangles">All values must be indecies within the range of <paramref name="vertices"/> and the length must be a multiple of 3</param>
        public MeshData(IEnumerable<Vector3> vertices, IEnumerable<Vector2> uvs, IEnumerable<int> triangles)
        {
            _v = vertices?.ToList() ?? new List<Vector3>();
            _u = uvs?.ToList() ?? new List<Vector2>();
            var c = _v.Count;
            if (c != _u.Count)
                throw new ArgumentException("vertices must be the same length as uvs", "uvs");
            _t = triangles?.ToList() ?? new List<int>();
            if (_t.Count % 3 != 0)
                throw new ArgumentException("triangles length must be a multiple of 3", "triangles");
            if (_t.Exists((x) => x >= c))
                throw new ArgumentException("triangles contains an index outside the vertices collection", "triangles");
        }
        /// <summary>Creates a duplicate of the <see cref="MeshData"/></summary>
        public MeshData Clone() => new MeshData(_v, _u, _t);
        /// <summary>Merges the data from the provided <see cref="MeshData"/> onto the current instance's data, effectively combining the meshes</summary>
        public void Add(MeshData data, MeshModifier modifier = null)
        {
            var c = _v.Count;
            if (!modifier)
                modifier = new MeshModifier();
            for (var i = 0; i < data._v.Count; i++)
            {
                _v.Add(modifier[data._v[i]]);
                _u.Add(modifier[data._u[i]]);
            }
            foreach (var i in data._t)
                    _t.Add(i + c);
        }
        /// <returns>A merged instance of the 2 data sets, effectively combining the meshes</returns>
        public static MeshData operator +(MeshData a, MeshData b)
        {
            a = a.Clone();
            a.Add(b);
            return a;
        }
        /// <summary>Modifies the data set based on the rules provided by the <paramref name="modifier"/></summary>
        public void Modify(MeshModifier modifier)
        {
            for (int i = 0; i < _v.Count; i++)
                _v[i] = modifier[_v[i]];
            for (int i = 0; i < _u.Count; i++)
                _u[i] = modifier[_u[i]];
        }
        /// <summary>Modifies the data set based on the rule provided by the <paramref name="modifier"/></summary>
        public void Modify(Func<Vector3, Vector2, (Vector3, Vector2)> modifier)
        {
            for (int i = 0; i < _v.Count; i++)
                (_v[i], _u[i]) = modifier(_v[i], _u[i]);
        }
        /// <returns>A modified instance of the data set based on the rules provided by the <see cref="MeshModifier"/></returns>
        public static MeshData operator *(MeshData a, MeshModifier b)
        {
            a = a.Clone();
            a.Modify(b);
            return a;
        }
        /// <returns>A modified instance of the data set based on the rules provided by the <see cref="MeshModifier"/></returns>
        public static MeshData operator *(MeshModifier a, MeshData b) => b * a;
        /// <returns>A modified instance of the data set based on the rule provided</returns>
        public static MeshData operator *(MeshData a, Func<Vector3, Vector2, (Vector3, Vector2)> b)
        {
            a = a.Clone();
            a.Modify(b);
            return a;
        }
        /// <returns>A modified instance of the data set based on the rule provided</returns>
        public static MeshData operator *(Func<Vector3, Vector2, (Vector3, Vector2)> a, MeshData b) => b * a;
        public static implicit operator MeshData(Mesh m) => new MeshData(m.vertices, m.uv, m.triangles);
        public static implicit operator Mesh(MeshData m) => m.ToMesh();
        /// <returns>A <see cref="Mesh"/> generated from the data set</returns>
        public Mesh ToMesh()
        {
            var m = new Mesh();
            m.vertices = vertices;
            m.uv = uvs;
            m.triangles = triangles;
            m.RecalculateBounds();
            m.RecalculateNormals();
            m.RecalculateTangents();
            return m;
        }
        public void RemoveWhere(Func<Vector3, Vector2, bool> predicate)
        {
            for (int i = 0; i < _v.Count - 1; i++)
                    if (predicate(_v[i], _u[i]))
                    {
                        _u.RemoveAt(i);
                        _v.RemoveAt(i);
                    for (int k = _t.Count - 3; k >= 0; k -= 3)
                        if (_t[k] == i || _t[k + 1] == i || _t[k + 2] == i)
                            _t.RemoveRange(k, 3);
                        else
                        {
                            if (_t[k] > i)
                                _t[k]--;
                            if (_t[k + 1] > i)
                                _t[k + 1]--;
                            if (_t[k + 2] > i)
                                _t[k + 2]--;
                        }
                    }
        }
        public void RemoveDuplicateVertices()
        {
            for (int i = 0; i < _v.Count - 1; i++)
                for (int j = _v.Count - 1; j > i; j--)
                    if (_v[i] == _v[j] && _u[i] == _u[j])
                    {
                        _u.RemoveAt(j);
                        _v.RemoveAt(j);
                        for (int k = 0;k < _t.Count; k++)
                            if (_t[k] == j)
                                _t[k] = i;
                            else if (_t[k] > j)
                                _t[k]--;
                    }
        }
        /// <summary>
        /// Merges the set of <see cref="MeshData"/> into a single <see cref="Mesh"/> with the data from each <see cref="MeshData"/> being registered as a submesh
        /// </summary>
        /// <returns>The generated mesh</returns>
        public static Mesh MergeAsSubMeshes(params MeshData[] data)
        {
            var m = Empty;
            foreach (var d in data)
                m += d;
            Mesh mesh = m;
            mesh.subMeshCount = data.Length;
            var c = 0;
            for (int i = 0; i < data.Length; i++)
            {
                mesh.SetSubMesh(i, new UnityEngine.Rendering.SubMeshDescriptor(c, data[i].triangles.Length));
                c += data[i].triangles.Length;
            }
            return mesh;
        }
    }

    /// <summary>For use with a <see cref="MeshData"/> instance to modify the vertices and or the uv map by defined rules</summary>
    public class MeshModifier
    {
        public readonly Func<Vector3, Vector3> vertices;
        public readonly Func<Vector2, Vector2> uvs;
        public MeshModifier(Func<Vector3, Vector3> vertexModifier = null, Func<Vector2, Vector2> uvModifier = null)
        {
            if (vertexModifier == null)
                vertexModifier = (x) => x;
            if (uvModifier == null)
                uvModifier = (x) => x;
            vertices = vertexModifier;
            uvs = uvModifier;
        }
        public static implicit operator MeshModifier(Func<Vector3, Vector3> v) => new MeshModifier(v, null);
        public static implicit operator MeshModifier(Func<Vector2, Vector2> v) => new MeshModifier(null, v);
        public static implicit operator Func<Vector3, Vector3>(MeshModifier v) => v.vertices;
        public static implicit operator Func<Vector2, Vector2>(MeshModifier v) => v.uvs;
        public static implicit operator bool(MeshModifier v) => v != null;
        public Vector3 this[Vector3 v] => vertices(v);
        public Vector2 this[Vector2 v] => uvs(v);
        /// <summary>
        /// <para>Combines 2 <see cref="MeshModifier"/>s</para>
        /// <para>Note: when used, <paramref name="a"/> is applied before <paramref name="b"/></para>
        /// </summary>
        public static MeshModifier operator +(MeshModifier a, MeshModifier b)
        {
            var av = a.vertices;
            var bv = b.vertices;
            var au = a.uvs;
            var bu = b.uvs;
            return new MeshModifier((x) => bv(av(x)), (x) => bu(au(x)));
        }
    }

    [HarmonyPatch(typeof(PathingNetwork), "Awake")]
    class Patch_PathingNetwork
    {
        public static bool prevent = false;
        static bool Prefix() => !prevent;
    }
}