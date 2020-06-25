//////////////////////////////////////////////////
// Externals.

using MartinVasina;     // Fire

//////////////////////////////////////////////////
// Rendering params.

Debug.Assert(scene != null);
Debug.Assert(context != null);

// Override image resolution and supersampling.
context[PropertyName.CTX_WIDTH]         = 640;
context[PropertyName.CTX_HEIGHT]        = 480;
context[PropertyName.CTX_SUPERSAMPLING] =  4;

//////////////////////////////////////////////////
// Preprocessing stage support.

// Uncomment the block if you need preprocessing.
if (Util.TryParseBool(context, PropertyName.CTX_PREPROCESSING))
{
  double time = 0.0;
  bool single = Util.TryParse(context, PropertyName.CTX_TIME, ref time);
  // if (single) simulate only for a single frame with the given 'time'

  // Preprocessing = simulation (run only once)
  Fire fire = new Fire(new Vector3d(0, 0, 0), 1.5);
  context["fire"] = fire;
}

//////////////////////////////////////////////////
// Param string from UI.

// Params dictionary.
Dictionary<string, string> p = Util.ParseKeyValueList(param);

//////////////////////////////////////////////////
// CSG scene.

AnimatedCSGInnerNode root = new AnimatedCSGInnerNode(SetOperation.Union);
root.SetAttribute(PropertyName.REFLECTANCE_MODEL, new PhongModel());
root.SetAttribute(PropertyName.MATERIAL, new PhongMaterial(new double[] {1.0, 0.7, 0.1}, 0.1, 0.85, 0.05, 64));
scene.Intersectable = root;

// Background color.
scene.BackgroundColor = new double[] {0.0, 0.01, 0.03};

scene.Camera = new StaticCamera(new Vector3d(0, 5, -5), new Vector3d(0.0, -0.8, 1), 50.0);

// Light sources.
scene.Sources = new System.Collections.Generic.LinkedList<ILightSource>();
scene.Sources.Add(new AmbientLightSource(0.8));
scene.Sources.Add(new PointLightSource(new Vector3d(-10, 15, 0), 1.2));


//scene.Sources.Add(new PointLightSource(new Vector3d(0, 1, 0), 0.1));

if (context.TryGetValue("fire", out object os) && os is Fire fire)
{
  root.InsertChild(fire, Matrix4d.Identity);
}
