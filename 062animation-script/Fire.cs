using MathSupport;
using OpenTK;
using Rendering;
using System;
using System.Collections.Generic;

namespace MartinVasina
{

  /// <summary>
  /// Fire simulation
  /// </summary>
  [Serializable]
  public class Fire : DefaultSceneNode, ISolid, ITimeDependent {
#if DEBUG
    private static volatile int nextSerial = 0;
    private readonly int serial = nextSerial++;
    public int getSerial () => serial;
#endif

    private static DeterministicRandom random = new DeterministicRandom();
    private static PhongMaterial pm = null;

    protected List<FireParticle> fireParticles_Base = new List<FireParticle>();
    protected List<FireParticle> fireParticles = new List<FireParticle>();

    private Vector3d POSITION = new Vector3d(0,0,0);
    private int BASE_PARTICLES_COUNT = 100;
    private int MAX_LIFETIME = 20;
    private double RADIUS = 1;
    private double PARTICLE_RADIUS = 0.05;
    private Vector3d PARTICLE_VELOCITY = new Vector3d(0,0.1,0);
    private double NOISE_DISPERSION = 8;

    private Fire() {
      // Set Material and NO_SHADOW
      SetMaterial();
    }

    public Fire (Vector3d position, double radius)
    {
      POSITION = position;
      RADIUS = radius;
      //BASE_PARTICLES_COUNT = (int) (100 * radius * radius);
      //MAX_LIFETIME = (int) (20 * radius);
      PARTICLE_RADIUS = 0.05 * radius;
      PARTICLE_VELOCITY *= radius;
     //NOISE_DISPERSION *= radius;

      // 1) nageneruji zakladnu
      FireParticle particle;
      for (int i = 0; i < BASE_PARTICLES_COUNT; ++i)
      {
        particle = new FireParticle(POSITION + MathUtils.CalculateRandomPositionInCircle(RADIUS,random), PARTICLE_RADIUS, PARTICLE_VELOCITY, NOISE_DISPERSION);
        fireParticles_Base.Add(particle);
      }

      particle = new FireParticle(POSITION, PARTICLE_RADIUS, PARTICLE_VELOCITY, NOISE_DISPERSION); // posledni castice je stredova
      fireParticles_Base.Add(particle);

      // 3) nastavim lifetime
      foreach (FireParticle fp in fireParticles_Base)
      {
        double distance = Vector3d.Distance(particle.GetPosition(), fp.GetPosition()); // vzdalenost od stredove castice
        fp.AddLifetime((int)((1 - (distance / radius)) * MAX_LIFETIME));
      }

      // 4) dostatek simulacnich kroku
      for (int i = 0; i <= MAX_LIFETIME; ++i)
        FireSimulationStep();

      // Set Material and NO_SHADOW
      SetMaterial();
    }

    private void SetMaterial()
    {
      // Set Material and NO_SHADOW
      if (pm == null)
      {
        pm = new PhongMaterial(new double[] { 0.886, 0.345, 0.133 }, 0.8, 0.0, 0, 128);
        pm.n = 1.0; //Absolute index of refraction
        pm.Kt = 1; //Coefficient of transparency.
      }
      SetAttribute(PropertyName.MATERIAL, pm);
      SetAttribute(PropertyName.NO_SHADOW, true);
    }


    // Provede jeden krok simulace ohne
    public void FireSimulationStep() {
      List<FireParticle> removable = new List<FireParticle>();
      // Odsimuluji vsechny fireParticles
      foreach (FireParticle fp in fireParticles)
      {
        if (fp.DecrementAndGetLifetime() < 0)
        {
          removable.Add(fp);
          continue;
        }
        fp.Move();
      }

      // Odstranim castice, kterym vyprsel lifetime
      foreach (FireParticle fp in removable)
        fireParticles.Remove(fp);

      // Naklonuji do fireParticles zaklad
      foreach (FireParticle fp in fireParticles_Base)
      {
        FireParticle clone = fp.Clone();
        fp.IncrementNoiseIndex(); //castici v zakladu zvetsim noiseIndex aby pristi klon mel o jedna vetsi noiseIndex
        clone.UpdateNoisePositionByNoiseIndex();
        fireParticles.Add(clone);
      }
    }



    // ------- ITimeDependent -------

    /// <summary>
    /// Starting (minimal) time in seconds.
    /// </summary>
    public double Start { get; set; }

    /// <summary>
    /// Ending (maximal) time in seconds.
    /// </summary>
    public double End { get; set; }

    private double oldTime = 0; // Rozdil mezi oldTime a time urcuje pocet simulacnich kroku, ktere se maji provest pri setTime
    protected double time;

    private int movesPerSecond = 16;

    /// <summary>
    /// Propagates time to descendants.
    /// Update object to the new time.. works only into the future
    /// </summary>
    protected virtual void setTime (double newTime)
    {
      int timeIndex = (int)(movesPerSecond * oldTime);
      int newTimeIndex = (int)(movesPerSecond * newTime);
      for (int i = timeIndex; i < newTimeIndex; ++i)
      {
        FireSimulationStep();
      }
      oldTime = newTime;
      time = newTime;
    }

    /// <summary>
    /// Current time in seconds.
    /// </summary>
    public double Time
    {
      get => time;
      set => setTime(value);
    }

    /// <summary>
    /// Clone all the time-dependent components, share the others.
    /// </summary>
    public virtual object Clone ()
    {
      Fire fireClone = new Fire();

      fireClone.POSITION = new Vector3d(this.POSITION);
      fireClone.BASE_PARTICLES_COUNT = this.BASE_PARTICLES_COUNT;
      fireClone.MAX_LIFETIME = this.MAX_LIFETIME;
      fireClone.RADIUS = this.RADIUS;
      fireClone.PARTICLE_RADIUS = this.PARTICLE_RADIUS;
      fireClone.PARTICLE_VELOCITY = new Vector3d(this.PARTICLE_VELOCITY);
      fireClone.NOISE_DISPERSION = this.NOISE_DISPERSION;

      foreach (FireParticle fp in this.fireParticles_Base)
        fireClone.fireParticles_Base.Add(fp.Clone());
      foreach (FireParticle fp in this.fireParticles)
        fireClone.fireParticles.Add(fp.Clone());

      fireClone.oldTime = this.time;
      fireClone.Time = time;

      return fireClone;
    }




    // ------- ISolid -------

    /// <summary>
    /// Computes the complete intersection of the given ray with all fire particles.
    /// </summary>
    /// <param name="p0">Ray origin.</param>
    /// <param name="p1">Ray direction vector.</param>
    /// <returns>Sorted list of intersection records.</returns>
    public override LinkedList<Intersection> Intersect (Vector3d p0, Vector3d p1)
    {
      //return Intersect(new Vector3d(x, y, z), 1 / radius, p0, p1);
      LinkedList<Intersection> result = new LinkedList<Intersection>();
      foreach (FireParticle fp in fireParticles)
      {
        LinkedList<Intersection> intersections = IntersectSphere(fp.GetActualPosition(), 1 / fp.GetRadius(), p0, p1);
        if (intersections != null)
        {
          foreach (var item in intersections)
            result.AddLast(item);
        }
      }

      return result;
    }

    /// <summary>
    /// Computes the complete intersection of the given ray with the sphere.
    /// </summary>
    /// <param name="offset">Sphere centre</param>
    /// <param name="scale">Sphere scale = (1 / radius)</param>
    /// <param name="p0">Ray origin.</param>
    /// <param name="p1">Ray direction vector.</param>
    /// <returns>Sorted list of intersection records.</returns>
    public LinkedList<Intersection> IntersectSphere (Vector3d offset, double scale, Vector3d p0, Vector3d p1)
    {
      Vector3d origin = (p0 - offset) * scale;
      Vector3d dir = p1 * scale;
      double OD;
      Vector3d.Dot(ref origin, ref dir, out OD);
      double DD;
      Vector3d.Dot(ref dir, ref dir, out DD);
      double OO;
      Vector3d.Dot(ref origin, ref origin, out OO);
      double d = OD * OD + DD * (1.0 - OO); // discriminant
      if (d <= 0.0)
        return null;            // no intersections

      // Single intersection: (-OD - d) / DD.
      LinkedList<Intersection> result = new LinkedList<Intersection>();
      double t = (-OD - Math.Sqrt(d)) / DD;

      Vector3d loc = p0 + t * p1;

      Intersection i = new Intersection(this)
      {
        T = t,
        Enter = true,
        Front = true,
        CoordLocal = loc,
        Normal = loc - offset,
      };
      result.AddLast(i);

      i = new Intersection(this)
      {
        T = t + Intersection.SHELL_THICKNESS,
        Enter = false,
        Front = false,
        CoordLocal = loc + Intersection.SHELL_THICKNESS * p1,
        Normal = loc - offset,
      };
      result.AddLast(i);

      return result;
    }

    /// <summary>
    /// Complete all relevant items in the given Intersection object.
    /// </summary>
    /// <param name="inter">Intersection instance to complete.</param>
    public override void CompleteIntersection (Intersection inter)
    {
      // Normal vector - no need to do anything here as NormalLocal is defined...

      // 2D texture coordinates.
      double r = Math.Sqrt(inter.CoordLocal.X * inter.CoordLocal.X + inter.CoordLocal.Y * inter.CoordLocal.Y);
      inter.TextureCoord.X = Geometry.IsZero(r)
        ? 0.0
        : (Math.Atan2(inter.CoordLocal.Y, inter.CoordLocal.X) / (2.0 * Math.PI) + 0.5);
      inter.TextureCoord.Y = Math.Atan2(r, inter.CoordLocal.Z) / Math.PI;
      //inter.TextureCoord.X = 0;

    }

    public void GetBoundingBox (out Vector3d corner1, out Vector3d corner2)
    {
      throw new NotImplementedException();
      //corner1 = new Vector3d(-1, -1, -1) * 2 * radius + new Vector3d(x, y, z);
      //corner2 = new Vector3d(1, 1, 1) * 2 * radius + new Vector3d(x, y, z);
    }

  }


















  public class FireParticle
  {
    private static DeterministicRandom random = new DeterministicRandom();

    private Vector3d position = new Vector3d(0,0,0);
    private Vector3d noisePosition = new Vector3d(0,0,0);
    private int noiseIndex = 0;
    private float noiseIndex2;
    private double radius = 0.05;
    private Vector3d velocity = new Vector3d(0, 0.1, 0);
    private int lifetime = 1;
    double noiseDispersion = 8;

    private FireParticle() { }

    // Vytvori novou castici ohne na dane pozici
    public FireParticle (Vector3d position, double radius, Vector3d velocity, double noiseDispersion)
    {
      this.position = position;
      this.radius = radius;
      this.velocity = new Vector3d(velocity);
      this.noiseDispersion = noiseDispersion;
      noiseIndex = (int)(random.NextDouble() * 100);
      noiseIndex2 = (float)(random.NextDouble() * 0.5 + 0.2);
      // Nastavi sumovou pozici jako vektor nahodneho uhlu s velikosti podle noise funkce z intervalu
      noisePosition = MathUtils.RotateVectorByRandomAngleY(new Vector3d(NoiseMaker.Noise(noiseIndex / 10f, noiseIndex2, 0) * noiseDispersion * radius, 0, 0), random);
    }

    // Vytvori klon castice
    public FireParticle Clone()
    {
      FireParticle cloneParticle =  new FireParticle();
      cloneParticle.velocity = new Vector3d(this.velocity);
      cloneParticle.position = new Vector3d(this.position);
      cloneParticle.noisePosition = new Vector3d(this.noisePosition);
      cloneParticle.noiseIndex = this.noiseIndex;
      cloneParticle.noiseIndex2 = this.noiseIndex2;
      cloneParticle.radius = this.radius;
      cloneParticle.lifetime = this.lifetime;
      cloneParticle.noiseDispersion = this.noiseDispersion;
      return cloneParticle;
    }

    // Skutecna poloha castice jako soucet position a noisePosition
    public Vector3d GetActualPosition()
    {
      return position + noisePosition;
    }

    public Vector3d GetPosition ()
    {
      return position;
    }

    public double GetRadius ()
    {
      return radius;
    }

    public void IncrementNoiseIndex ()
    {
      ++noiseIndex;
    }

    // Pricte k lifetime
    public void AddLifetime(int value)
    {
      this.lifetime += value;
    }

    // Snizi lifetime a vrati jeho hodnotu
    public int DecrementAndGetLifetime ()
    {
      return --lifetime;
    }

    // Provede simulacni krok castice
    // Zmeni position podle velocity
    // Nastavi velikost noisePosition podle sumove funkce
    public void Move()
    {
      position += velocity;
    }

    public void UpdateNoisePositionByNoiseIndex()
    {
      MathUtils.SetVectorLength(ref noisePosition, NoiseMaker.Noise(noiseIndex / 10f, noiseIndex2, 0) * noiseDispersion * radius);
    }
  }
}
