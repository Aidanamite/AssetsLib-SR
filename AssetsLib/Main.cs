using HarmonyLib;
using SRML;
using SRML.Console;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using SRML.SR;
using SRML.SR.Translation;
using TMPro;
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
        /// <para>Creates a <see cref="Mesh"/> using the provided vertex, uv and triangle data. For each item in the <paramref name="modifiers"/> a duplicate of the mesh, depicted by the
        /// data provided, is created, applying the respective modifier to the vertices of the duplicate.</para>
        /// <para>This is designed to be used for generating a single <see cref="Mesh"/> that is made up of several duplicates of another mesh.</para>
        /// <para>Throws an <see cref="ArgumentException"/> if the length of the <paramref name="vertices"/> is different to the length of the <paramref name="uv"/></para>
        /// <para>Throws an <see cref="IndexOutOfRangeException"/> if one of the triangle indices is outside the <paramref name="vertices"/> array</para>
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
        /// <para>Creates a <see cref="Mesh"/> using the provided <see cref="MeshData"/> object. For each item in the modifiers a duplicate of the mesh, depicted by the
        /// data provided, is created, applying the respective modifier to the duplicate.</para>
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
        /// <para>Creates a <see cref="Mesh"/> using the provided vertex, uv and triangle data. Points in the mesh that match
        /// the <paramref name="removeAt"/> predicate will be removed, all other points will be modified by the specified modify function</para>
        /// <para>This is designed to be used for tweaking a <see cref="Mesh"/></para>
        /// <para>Throws an <see cref="ArgumentException"/> if the length of the <paramref name="vertices"/> is different to the length of the <paramref name="uv"/></para>
        /// <para>Throws an <see cref="IndexOutOfRangeException"/> if one of the triangle indices is outside the <paramref name="vertices"/> array</para>
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
        /// <para>This is designed to be used for generating the bone weights and configuring the <see cref="SlimeAppearanceObject"/>s for a custom slime model</para>
        /// <para>Note: this only works for <see cref="SlimeAppearanceObject"/>s that use a <see cref="SkinnedMeshRenderer"/>. <see cref="MeshFilter"/>s will not be affected</para>
        /// </summary>
        /// <param name="slimePrefab">the <see cref="SlimeAppearanceApplicator"/> from your slime prefab</param>
        /// <param name="bodyApp">the <see cref="SlimeAppearanceObject"/> of your slime's main body</param>
        /// <param name="jiggleAmount">the amount the model will be affected by the slime's movement</param>
        /// <param name="scale">the scale to put the model to. This is handy due to model's scale being unaffected by <see cref="Transform.localScale"/></param>
        /// <param name="appearanceObjects">the additional <see cref="SlimeAppearanceObject"/>s that you want the weight data and configuration to be put on</param>
        public static void GenerateBoneData(SlimeAppearanceApplicator slimePrefab, SlimeAppearanceObject bodyApp, float jiggleAmount = 1, float scale = 1, params SlimeAppearanceObject[] appearanceObjects) => GenerateBoneData(slimePrefab, bodyApp, jiggleAmount, scale, null, appearanceObjects);

        /// <summary>
        /// <para>Generates a basic set of bone data for provided <see cref="Mesh"/>es and <see cref="SlimeAppearanceObject"/>s</para>
        /// <para>This is designed to be used for generating the bone weights and configuring the <see cref="SlimeAppearanceObject"/>s for a custom slime model</para>
        /// <para>Note: this only works for <see cref="SlimeAppearanceObject"/>s that use a <see cref="SkinnedMeshRenderer"/>. <see cref="MeshFilter"/>s will not be affected</para>
        /// </summary>
        /// <param name="slimePrefab">the <see cref="SlimeAppearanceApplicator"/> from your slime prefab</param>
        /// <param name="bodyApp">the <see cref="SlimeAppearanceObject"/> of your slime's main body</param>
        /// <param name="jiggleAmount">the amount the model will be affected by the slime's movement</param>
        /// <param name="scale">the scale to put the model to. This is handy due to model's scale being unaffected by <see cref="Transform.localScale"/></param>
        /// <param name="AdditionalMesh">an array of additional <see cref="Mesh"/>es that you want the weight data to be put on</param>
        public static void GenerateBoneData(SlimeAppearanceApplicator slimePrefab, SlimeAppearanceObject bodyApp, float jiggleAmount = 1, float scale = 1, params Mesh[] AdditionalMesh) => GenerateBoneData(slimePrefab, bodyApp, jiggleAmount, scale, AdditionalMesh, null);

        /// <summary>
        /// <para>Generates a basic set of bone data for provided <see cref="Mesh"/>es and <see cref="SlimeAppearanceObject"/>s</para>
        /// <para>This is designed to be used for generating the bone weights and configuring the <see cref="SlimeAppearanceObject"/>s for a custom slime model</para>
        /// <para>Note: this only works for <see cref="SlimeAppearanceObject"/>s that use a <see cref="SkinnedMeshRenderer"/>. <see cref="MeshFilter"/>s will not be affected</para>
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
                foreach (var o in s.Element.Prefabs)
                    if (o.GetComponent<Renderer>() is SkinnedMeshRenderer && !objs.Contains(o))
                        objs.Add(o);
            if (objs.Count == 0)
                throw new ArgumentException("The provided SlimeAppearance does not contain any SkinnedMeshRenderers", "appearance");
            var body = objs.First();
            objs.RemoveAt(0);
            GenerateBoneData(slimePrefab, body, jiggleAmount, scale, AdditionalMesh, objs.ToArray());
        }

        /// <summary>
        /// <para>Generates a basic set of bone data for provided <see cref="Mesh"/>es and <see cref="SlimeAppearanceObject"/>s</para>
        /// <para>This is designed to be used for generating the bone weights and configuring the <see cref="SlimeAppearanceObject"/>s for a custom slime model</para>
        /// <para>Note: this only works for <see cref="SlimeAppearanceObject"/>s that use a <see cref="SkinnedMeshRenderer"/>. <see cref="MeshFilter"/>s will not be affected</para>
        /// </summary>
        /// <param name="slimePrefab">the <see cref="SlimeAppearanceApplicator"/> from your slime prefab</param>
        /// <param name="bodyApp">the <see cref="SlimeAppearanceObject"/> of your slime's main body</param>
        /// <param name="jiggleAmount">the amount the model will be affected by the slime's movement</param>
        /// <param name="scale">the scale to put the model to. This is handy due to model's scale being unaffected by <see cref="Transform.localScale"/></param>
        /// <param name="AdditionalMesh">an array of additional <see cref="Mesh"/>es that you want the weight data to be put on</param>
        /// <param name="appearanceObjects">the additional <see cref="SlimeAppearanceObject"/>s that you want the weight data and configuration to be put on</param>
        public static void GenerateBoneData(SlimeAppearanceApplicator slimePrefab, SlimeAppearanceObject bodyApp, float jiggleAmount = 1, float scale = 1, Mesh[] AdditionalMesh = null, params SlimeAppearanceObject[] appearanceObjects)
        {
            if (AdditionalMesh == null)
                AdditionalMesh = new Mesh[0];
            var mesh = bodyApp.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            bodyApp.AttachedBones = new SlimeAppearance.SlimeBone[] { SlimeAppearance.SlimeBone.Slime, SlimeAppearance.SlimeBone.JiggleRight, SlimeAppearance.SlimeBone.JiggleLeft, SlimeAppearance.SlimeBone.JiggleTop, SlimeAppearance.SlimeBone.JiggleBottom, SlimeAppearance.SlimeBone.JiggleFront, SlimeAppearance.SlimeBone.JiggleBack };
            foreach (var a in appearanceObjects)
                a.AttachedBones = bodyApp.AttachedBones;
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
            meshes.AddRange(AdditionalMesh);
            foreach (var m in meshes)
            {
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
            Texture2D texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, source.mipmapCount, true);
            Graphics.CopyTexture(source, texture);
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
        /// <para>For use with the purchase UI</para>
        /// </summary>
        /// <param name="ui">the <see cref="GameObject"/> of the UI</param>
        /// <param name="refresh">if <see langword="true"/>, the UI is refreshed on purchase. if <see langword="false"/> the UI is closed on purchase</param>
        /// <param name="action">the code to run on purchase</param>
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
            //ui.transform.Find("MainPanel/CloseButton").GetComponent<Button>().onClick.AddListener(ui.Close);
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
    }

    public interface IInventoryItem
    {
        bool OnClick();
        void RefreshEntry(GameObject gO);
    }

    public class IdentInventoryItem : IInventoryItem
    {
        Identifiable.Id id;
        int count;
        Func<bool> onClick;
        public IdentInventoryItem(Identifiable.Id Id, int Count, Func<bool> OnClick = null)
        {
            id = Id;
            count = Count;
            onClick = OnClick;
        }
        bool IInventoryItem.OnClick() => onClick?.Invoke() ?? false;
        void IInventoryItem.RefreshEntry(GameObject gO)
        {
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
        /// <summary>
        /// <para>Finds an effect prefab given its <paramref name="name"/></para>
        /// </summary>
        /// <returns>The effect prefab</returns>
        /// <param name="name">the name of the effect prefab to search for</param>
        public static GameObject FindFX(string name) => SceneContext.Instance.fxPool.pooledObjects.Keys.First((x) => x.name == name);

        /// <summary>
        /// <para>Finds an effect prefab given a <see cref="Predicate{T}"/></para>
        /// </summary>
        /// <returns>The effect prefab</returns>
        /// <param name="predicate">the predicate to search for a match for</param>
        public static GameObject FindFX(Predicate<GameObject> predicate) => SceneContext.Instance.fxPool.pooledObjects.Keys.First((x) => predicate(x));

        /// <summary>
        /// <para>Finds an object of type <typeparamref name="T"/> given its <paramref name="name"/></para>
        /// </summary>
        /// <returns>The first object found</returns>
        /// <param name="name">the name of the object to search for</param>
        public static T FindObjectByName<T>(string name) where T : Object => Resources.FindObjectsOfTypeAll<T>().FirstOrDefault((x) => x.name == name);

        /// <summary>
        /// <para>Finds multi objects of type <typeparamref name="T"/> given their <paramref name="names"/></para>
        /// </summary>
        /// <returns>An array of the first object found with each name</returns>
        /// <param name="names">the names of the objects to search for</param>
        public static T[] FindObjectsByNames<T>(params string[] names) where T : Object => FindObjectsByNames<T>((IEnumerable<string>)names);

        /// <summary>
        /// <para>Finds multi objects of type <typeparamref name="T"/> given their <paramref name="names"/></para>
        /// </summary>
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

        /// <summary>
        /// <para>Finds an object of type <typeparamref name="T"/> given a <see cref="Predicate{T}"/></para>
        /// </summary>
        /// <returns>The first object found</returns>
        /// <param name="predicate">the predicate to search for a match for</param>
        public static T FindObject<T>(Predicate<T> predicate) where T : Object => Resources.FindObjectsOfTypeAll<T>().FirstOrDefault((x) => predicate(x));

        /// <summary>
        /// <para>Finds multi objects of type <typeparamref name="T"/> given a set of <see cref="Predicate{T}"/>s</para>
        /// </summary>
        /// <returns>An array of the first object matching each <see cref="Predicate{T}"/></returns>
        /// <param name="predicates">the predicate to search for a match for</param>
        public static T[] FindObjects<T>(params Predicate<T>[] predicates) where T : Object => FindObjects((IEnumerable<Predicate<T>>)predicates);

        /// <summary>
        /// <para>Finds multi objects of type <typeparamref name="T"/> given a set of <see cref="Predicate{T}"/>s</para>
        /// </summary>
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


        /// <summary>
        /// <para>Creates a <see cref="SlimeAppearanceElement"/></para>
        /// </summary>
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

        /// <returns>The <see cref="SlimeAppearance"/> of a slime given an <see cref="SlimeAppearance.AppearanceSaveSet"/></returns>
        public static SlimeAppearance GetAppearance(this Identifiable.Id id, SlimeAppearance.AppearanceSaveSet saveSet) => SceneContext.Instance.SlimeAppearanceDirector.SlimeDefinitions.GetSlimeByIdentifiableId(id).GetAppearanceForSet(saveSet);

        /// <returns>The prefab of the <see cref="Identifiable.Id"/> from the <see cref="LookupDirector"/></returns>
        public static GameObject GetPrefab(this Identifiable.Id id) => GameContext.Instance.LookupDirector.GetPrefab(id);

        /// <summary>
        /// <para>Attepts to get a <see cref="Component"/> of type <typeparamref name="T"/>. If the <see cref="Component"/> was not present on the <see cref="GameObject"/> it is added.</para>
        /// </summary>
        /// <returns>An instance of <typeparamref name="T"/></returns>
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component => obj.GetComponent<T>() ?? obj.AddComponent<T>();

        /// <summary>
        /// <para>Attepts to get a <see cref="Component"/> of type <typeparamref name="T"/>. If the <see cref="Component"/> was not present on the <see cref="GameObject"/> it is added.</para>
        /// </summary>
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
        /// <returns>Instantiates an inactive duplicate of the object</returns>
        public static GameObject CreateInactive(this GameObject obj)
        {
            var o = Object.Instantiate(obj, Main.prefabParent, true);
            o.SetActive(false);
            o.transform.SetParent(null, true);
            return o;
        }

        public static T CreateScriptableObject<T>(Action<T> construct = null) where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            construct?.Invoke(o);
            return o;
        }
    }

    public static class ExtentionMethods
    {

        /// <summary>
        /// <para>Searches for an item matching the provided <see cref="Predicate{T}"/>. Starts searching at <paramref name="start"/>. if no matching value is found after <paramref name="start"/> then it will search from the first item up to <paramref name="start"/></para>
        /// </summary>
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
        /// <para>If index is outside the bounds of the list then it will be wrapped to the list's length. For example, -1 will be the last item in the list</para>
        /// </summary>
        /// <returns>The item at the specified index</returns>
        /// <param name="index">the index of the item to fetch</param>
        public static T Get<T>(this List<T> s, int index) => s[index.Mod(s.Count)];

        /// <summary>
        /// <para>Gets the result of a non-negative modular division</para>
        /// </summary>
        public static int Mod(this int o, int v) => o % v + (o < 0 ? v : 0);


        /// <summary>
        /// <para>Attempts to find an item within a list. If the item is found its index is returned, otherwise the item is added to the list and the new item's index is returned</para>
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

        /// <summary>
        /// <para>Recursively searchs all the children of the <see cref="Transform"/> to find all that have a certain name</para>
        /// </summary>
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

        /// <summary>
        /// <para>Adds an id to the <see cref="SlimeEat.FoodGroup"/></para>
        /// </summary>
        public static void AddItem(this SlimeEat.FoodGroup foodGroup, Identifiable.Id ident) => SlimeEat.foodGroupIds[foodGroup] = SlimeEat.foodGroupIds.TryGetValue(foodGroup, out var v) ? v.AddToArray(ident) : new Identifiable.Id[] { ident };

        /// <summary>
        /// <para>Rotates a <see cref="Vector3"/> by the provided <see cref="Quaternion"/></para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Quaternion rotation) => rotation * value;
        /// <summary>
        /// <para>Rotates a <see cref="Vector3"/> by the provided Euler</para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Vector3 rotation) => value.Rotate(Quaternion.Euler(rotation));
        /// <summary>
        /// <para>Rotates a <see cref="Vector3"/> by the provided Euler</para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, float x, float y, float z) => value.Rotate(Quaternion.Euler(x, y, z));
        /// <summary>
        /// <para>Rotates a <see cref="Vector3"/> by the provided <see cref="Quaternion"/> around the specified point</para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Quaternion rotation, Vector3 rotatePoint) => value.Offset(-rotatePoint).Rotate(rotation).Offset(rotatePoint);
        /// <summary>
        /// <para>Rotates a <see cref="Vector3"/> by the provided Euler around the specified point</para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Vector3 rotation, Vector3 rotatePoint) => value.Rotate(Quaternion.Euler(rotation), rotatePoint);
        /// <summary>
        /// <para>Rotates a <see cref="Vector3"/> by the provided Euler around the specified point</para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, float x, float y, float z, Vector3 rotatePoint) => value.Rotate(Quaternion.Euler(x, y, z), rotatePoint);
        /// <summary>
        /// <para>Rotates a <see cref="Vector3"/> by the provided <see cref="Quaternion"/> around the specified point</para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Quaternion rotation, float rotatePointX, float rotatePointY, float rotatePointZ) => value.Rotate(rotation, new Vector3(rotatePointX, rotatePointY, rotatePointZ));
        /// <summary>
        /// <para>Rotates a <see cref="Vector3"/> by the provided Euler around the specified point</para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, Vector3 rotation, float rotatePointX, float rotatePointY, float rotatePointZ) => value.Rotate(Quaternion.Euler(rotation), new Vector3(rotatePointX, rotatePointY, rotatePointZ));
        /// <summary>
        /// <para>Rotates a <see cref="Vector3"/> by the provided Euler around the specified point</para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector3"/></returns>
        public static Vector3 Rotate(this Vector3 value, float x, float y, float z, float rotatePointX, float rotatePointY, float rotatePointZ) => value.Rotate(Quaternion.Euler(x, y, z), new Vector3(rotatePointX, rotatePointY, rotatePointZ));
        /// <summary>
        /// <para>Offsets a <see cref="Vector3"/> by the provided <see cref="Vector3"/></para>
        /// </summary>
        /// <returns>The offset <see cref="Vector3"/></returns>
        public static Vector3 Offset(this Vector3 value, float x, float y, float z) => value.Offset(new Vector3(x, y, z));
        /// <summary>
        /// <para>Offsets a <see cref="Vector3"/> by the provided <see cref="Vector3"/></para>
        /// </summary>
        /// <returns>The offset <see cref="Vector3"/></returns>
        public static Vector3 Offset(this Vector3 value, Vector3 offset) => value + offset;
        /// <summary>
        /// <para>Multiplies a <see cref="Vector3"/> by the provided <see cref="Vector3"/></para>
        /// </summary>
        /// <returns>The scaled <see cref="Vector3"/></returns>
        public static Vector3 Multiply(this Vector3 value, float x, float y, float z) => new Vector3(value.x * x, value.y * y, value.z * z);
        /// <summary>
        /// <para>Multiplies a <see cref="Vector3"/> by the provided <see cref="Vector3"/></para>
        /// </summary>
        /// <returns>The scaled <see cref="Vector3"/></returns>
        public static Vector3 Multiply(this Vector3 value, float scale) => value.Multiply(scale, scale, scale);
        /// <summary>
        /// <para>Multiplies a <see cref="Vector3"/> by the provided <see cref="Vector3"/></para>
        /// </summary>
        /// <returns>The scaled <see cref="Vector3"/></returns>
        public static Vector3 Multiply(this Vector3 value, Vector3 scale) => value.Multiply(scale.x, scale.y, scale.z);
        /// <summary>
        /// <para>Multiplies a <see cref="Vector2"/> by the provided <see cref="Vector2"/></para>
        /// </summary>
        /// <returns>The scaled <see cref="Vector2"/></returns>
        public static Vector2 Multiply(this Vector2 value, float x, float y) => new Vector2(value.x * x, value.y * y);
        /// <summary>
        /// <para>Multiplies a <see cref="Vector2"/> by the provided <see cref="Vector2"/></para>
        /// </summary>
        /// <returns>The scaled <see cref="Vector2"/></returns>
        public static Vector2 Multiply(this Vector2 value, Vector2 scale) => value.Multiply(scale.x, scale.y);
        /// <summary>
        /// <para>Offsets a <see cref="Vector2"/> by the provided <see cref="Vector2"/></para>
        /// </summary>
        /// <returns>The offset <see cref="Vector2"/></returns>
        public static Vector2 Offset(this Vector2 value, float x, float y) => value.Offset(new Vector2(x, y));
        /// <summary>
        /// <para>Offsets a <see cref="Vector2"/> by the provided <see cref="Vector2"/></para>
        /// </summary>
        /// <returns>The offset <see cref="Vector2"/></returns>
        public static Vector2 Offset(this Vector2 value, Vector2 offset) => value + offset;
        /// <summary>
        /// <para>Rotates a <see cref="Vector2"/> by the provided angle in degrees</para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector2"/></returns>
        public static Vector2 Rotate(this Vector2 value, float angle)
        {
            var l = value.magnitude;
            var a = Mathf.Atan2(value.x, value.y) + angle / 180 * Mathf.PI;
            if (value.y < 0)
                a += Mathf.PI;
            return new Vector2(Mathf.Sin(a) * l, Mathf.Cos(a) * l);
        }
        /// <summary>
        /// <para>Rotates a <see cref="Vector2"/> by the provided angle in degrees around the provided point</para>
        /// </summary>
        /// <returns>The rotated <see cref="Vector2"/></returns>
        public static Vector2 Rotate(this Vector2 value, float angle, Vector2 rotatePoint) => value.Offset(-rotatePoint).Rotate(angle).Offset(rotatePoint);
        /// <summary>
        /// <para>Rotates a <see cref="Vector2"/> by the provided angle in degrees around the provided point</para>
        /// </summary>
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


        /// <summary>
        /// <para>Copies all the field's values from <paramref name="b"/>.</para>
        /// </summary>
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

        /// <summary>
        /// Overlays the <see cref="Color"/> with <paramref name="color"/> with respect to alpha values
        /// </summary>
        /// <param name="c"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color Overlay(this Color c, Color color) => new Color(c.r * (1 - color.a) + color.r * color.a, c.g * (1 - color.a) + color.g * color.a, c.b * (1 - color.a) + color.b * color.a, Mathf.Max(c.a, color.a));

        public static Color Multiply(this Color color, float r, float g, float b) => new Color(color.r * r, color.g * g, color.b * b, color.a);
        public static Color Multiply(this Color color, float m) => new Color(color.r * m, color.g * m, color.b * m, color.a);

        /// <summary>Returns the <see cref="Color"/> grayscaled then tinted</summary>
        /// <param name="color"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Color Shift(this Color color, float r, float g, float b)
        {
            var v = color.grayscale;
            return new Color(v * r, v * g, v * b, color.a);
        }

        /// <summary>
        /// <para>Selects a random object from a collection given the weights fetched from the <paramref name="getWeight"/> function</para>
        /// </summary>
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

    }

    /// <summary>A set of <see cref="Color"/>s that behaves similar to a spectrum or gradient</summary>
    public class ColorGroup
    {
        List<Color> colors = new List<Color>();
        List<float> positions = new List<float>();

        /// <summary>
        /// <para>Adds a <see cref="Color"/> to the <see cref="ColorGroup"/> at the specified position</para>
        /// </summary>
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
            _v = vertices.ToList();
            _u = uvs.ToList();
            var c = _v.Count;
            if (c != _u.Count)
                throw new ArgumentException("vertices must be the same length as uvs", "uvs");
            _t = triangles.ToList();
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
        /// <returns>A modified instance of the data set based on the rules provided by the <see cref="MeshModifier"/></returns>
        public static MeshData operator *(MeshData a, MeshModifier b)
        {
            a = a.Clone();
            a.Modify(b);
            return a;
        }
        /// <returns>A modified instance of the data set based on the rules provided by the <see cref="MeshModifier"/></returns>
        public static MeshData operator *(MeshModifier a, MeshData b) => b * a;
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
        public void RemoveDuplicateVertices()
        {
            for (int i = 0; i < _v.Count - 1; i++)
                for (int j = _v.Count - 1; j > i; j--)
                    if (_v[i] == _v[j] && _u[i] == _u[j])
                    {
                        _u.RemoveAt(j);
                        _v.RemoveAt(j);
                        for (int k = _t.Count-3;k >= 0;k-=3)
                            if (_t[k] == j || _t[k + 1] == j || _t[k + 2] == j)
                                _t.RemoveRange(k, 3);
                            else
                            {
                                if (_t[k] > j)
                                    _t[k]--;
                                if (_t[k + 1] > j)
                                    _t[k + 1]--;
                                if (_t[k + 2] > j)
                                    _t[k + 2]--;
                            }
                    }
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
}