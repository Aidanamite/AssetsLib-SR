
        /// <summary>
        /// Lighting options for the object rendering methods:<br/>
        /// <see cref="RenderImage(GameObject, RenderConfig, out Exception, bool, LightingMode, int)"/><br/>
        /// <see cref="RenderImages(GameObject, RenderConfig[], out Exception[], bool, LightingMode, int)"/>
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
        /// <see cref="RenderImage(GameObject, RenderConfig, out Exception, bool, LightingMode, int)"/><br/>
        /// <see cref="RenderImages(GameObject, RenderConfig[], out Exception[], bool, LightingMode, int)"/>
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
            public RenderConfig(uint width,uint height,Quaternion angle)
            {
                imgWidth = width;
                imgHeight = height;
                cameraAngle = angle;
            }
        }

        /// <summary>
        /// Renders an image of the object using the provided configuration.
        /// </summary>
        /// <param name="copyObjectForRender">if <see langword="true"/>, it will instantiate the object and render the instantiated copy, otherwise will render the object itself. This will almost always need to be enabled for rendering a prefab</param>
        /// <param name="exception">Will be set to whatever <see cref="Exception"/> occured while trying to render, if one occured. Otherwise will be <see langword="null"/></param>
        /// <param name="go">The object to render an image of</param>
        /// <param name="lightingMode">Which light sources should be included in the render</param>
        /// <param name="renderConfig">The configuration to use for rendering the image</param>
        /// <param name="renderLayer">The layer to use for the render. It is recommended to leave as 30 to avoid unwanted object being included in the image</param>
        /// <returns><see langword="null"/> and sets the <paramref name="exception"/> value if an <see cref="Exception"/> occured, otherwise the generated image</returns>
        public static Texture2D RenderImage(this GameObject go, RenderConfig renderConfig, out Exception exception, bool copyObjectForRender = true, LightingMode lightingMode = LightingMode.DoNotChange, int renderLayer = 30)
        {
            var r = go.RenderImages(new[] { renderConfig },out var exceptions, copyObjectForRender,lightingMode,renderLayer)[0];
            exception = exceptions[0];
            return r;
        }
        /// <summary>
        /// Renders multiple images of the object using the provided configurations.
        /// </summary>
        /// <param name="copyObjectForRender">if <see langword="true"/>, it will instantiate the object and render the instantiated copy, otherwise will render the object itself. This will almost always need to be enabled for rendering a prefab</param>
        /// <param name="exceptions">Will be set to an array of the same size as <paramref name="renderConfigs"/>. For any renders that fail, the error that occured will be stored at the respective index</param>
        /// <param name="go">The object to render an image of</param>
        /// <param name="lightingMode">Which light sources should be included in the render</param>
        /// <param name="renderConfigs">The configurations to use for rendering the images</param>
        /// <param name="renderLayer">The layer to use for the render. It is recommended to leave as 30 to avoid unwanted object being included in the image</param>
        /// <returns>An array of the same size as <paramref name="renderConfigs"/> containing the images generated, respective of the indecies. If an error occured while generating a particular image, a <see langword="null"/> will be stored at that index and the exception will be stored in <paramref name="exceptions"/> at said index</returns>
        public static Texture2D[] RenderImages(this GameObject go, RenderConfig[] renderConfigs, out Exception[] exceptions, bool copyObjectForRender = true, LightingMode lightingMode = LightingMode.DoNotChange, int renderLayer = 30)
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
                c.nearClipPlane = 0;
                c.useOcclusionCulling = false;
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
                        //Before render call
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
                        var s = (float)Math.Ceiling(Math.Max((max - min).magnitude, (new Vector2(max.x, max.z) - new Vector2(min.x, min.z)).magnitude));
                        if (float.IsNaN(s) || float.IsInfinity(s))
                            throw new InvalidOperationException("Failed to detect valid object bounds");
                        RenderSettings.ambientLight = config.ambientLight ?? originalLight;
                        c.backgroundColor = config.backgroundColor;
                        c.aspect = (float)config.imgWidth / config.imgHeight;
                        var dir = config.cameraAngle * Vector3.forward;
                        c.transform.position = ((max + min) / 2) + (-dir * s * 2);
                        c.transform.rotation = config.cameraAngle;
                        c.orthographicSize = s / 2;
                        c.farClipPlane = s * 8;
                        var r = RenderTexture.GetTemporary(innerWidth, innerHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                        c.pixelRect = new Rect(0, 0, r.width * 4, r.height * 4);
                        created.Add(r);
                        c.targetTexture = r;
                        c.Render();
                        var prev = RenderTexture.active;
                        RenderTexture.active = r;
                        var texture = new Texture2D((int)config.imgWidth, (int)config.imgHeight, TextureFormat.ARGB32, config.mipmapCount,config.linearImg);
                        texture.ReadPixels(new Rect(0, 0, r.width, r.height), (int)(config.imageMargins?.left ?? 0), (int)(config.imageMargins?.bottom ?? 0));
                        if (config.imageMargins != null)
                        {
                            var edge = texture.FindEdges();
                            if (edge.minX > config.imageMargins.Value.left && edge.minY > config.imageMargins.Value.bottom && edge.maxX < config.imgWidth - config.imageMargins.Value.right - 1 && edge.maxY < config.imgHeight - config.imageMargins.Value.top - 1)
                            {
                                var scale = Math.Max((edge.maxX - edge.minX + 1) / (float)r.width, (edge.maxY - edge.minY + 1) / (float)r.height);
                                var off = new Vector2((edge.maxX - edge.minX + 1 - r.width) / 2f, r.height / 2);
                                c.transform.position += c.transform.up * ((edge.maxY + edge.minY - r.height) / 2f / r.height * s) + c.transform.right * ((edge.maxX + edge.minX - r.width) / 2f / r.height * s);
                                c.orthographicSize *= scale;
                                c.Render();
                                texture.ReadPixels(new Rect(0, 0, r.width, r.height), (int)(config.imageMargins?.left ?? 0), (int)(config.imageMargins?.bottom ?? 0));
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
                            Object.Destroy(i);
                    }
                Time.timeScale = originalTimeScale;
                Time.fixedDeltaTime = originalFixedTimeScale;
                RenderSettings.skybox = originalSkybox;
                RenderSettings.ambientLight = originalLight;
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

        internal static (int minX, int minY, int maxX, int maxY) FindEdges(this Texture2D texture)
        {
            var x1 = texture.height - 1;
            var y1 = texture.height - 1;
            var x2 = 0;
            var y2 = 0;
            for (int x = 0; x < texture.height; x++)
                for (int y = 0; y < texture.width; y++)
                    if (texture.GetPixel(x, y).a > 0)
                    {
                        x1 = Math.Min(x1, x);
                        x2 = Math.Max(x2, x);
                        y1 = Math.Min(y1, y);
                        y2 = Math.Max(y2, y);
                    }
            return (x1, y1, x2, y2);
        }