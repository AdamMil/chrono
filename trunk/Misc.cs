using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.Serialization;

namespace Chrono
{

public class EmptyEnumerator : System.Collections.IEnumerator
{ public object Current { get { throw new InvalidOperationException(); } }
  public bool MoveNext() { return false; }
  public void Reset() { }
}

public enum Direction
{ Up=0, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft,
  Above, Below, Self, Invalid
};

public enum TraceAction { Stop=1, Go=2, HBounce=4, VBounce=8, Bounce=HBounce|VBounce };
public struct TraceResult
{ public TraceResult(Point pt, Point prev) { Point=pt; Previous=prev; }
  public Point Point;
  public Point Previous;
}
public delegate TraceAction LinePoint(Point point, object context);

[Serializable]
public sealed class ObjectProxy : ISerializable, IObjectReference
{ public ObjectProxy(SerializationInfo info, StreamingContext context) { ID=info.GetUInt64("ID"); }
  public void GetObjectData(SerializationInfo info, StreamingContext context) { } // never called
  public object GetRealObject(StreamingContext context) { return Global.ObjHash[ID]; }
  ulong ID;
}

public class UniqueObject : ISerializable
{ public UniqueObject() { ID=Global.NextID; }
  protected UniqueObject(SerializationInfo info, StreamingContext context)
  { Type t = GetType();
    do // private fields are not inherited, so we traverse the class hierarchy ourselves
    { foreach(FieldInfo f in t.GetFields(BindingFlags.DeclaredOnly|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
        if(!f.IsNotSerialized) f.SetValue(this, info.GetValue(f.Name, f.FieldType));
      t = t.BaseType;
    } while(t!=null);

    if(Global.ObjHash!=null) Global.ObjHash[ID] = this;
  }
  public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
  { if(Global.ObjHash!=null && Global.ObjHash.Contains(ID))
    { info.AddValue("ID", ID);
      info.SetType(typeof(ObjectProxy));
    }
    else
    { if(Global.ObjHash!=null) Global.ObjHash[ID] = this;
      Type t = GetType();
      do // private fields are not inherited, so we traverse the class hierarchy ourselves
      { foreach(FieldInfo f in t.GetFields(BindingFlags.DeclaredOnly|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
          if(!f.IsNotSerialized) info.AddValue(f.Name, f.GetValue(this));
        t = t.BaseType;
      } while(t!=null);
    }
  }
  
  public ulong ID;
}

public sealed class Global
{ private Global() { }

  public static ulong NextID { get { return nextID++; } }

  public static string AorAn(string s)
  { char fc = char.ToLower(s[0]);
    if(fc=='a' || fc=='e' || fc=='i' || fc=='o' || fc=='u') return "an";
    else return "a";
  }

  public static string Cap1(string s)
  { if(s.Length==0) return s;
    string ret = char.ToUpper(s[0]).ToString();
    if(s.Length>1) ret += s.Substring(1);
    return ret;
  }

  public static bool Coinflip() { return Random.Next(100)<50; }

  public static Point Move(Point pt, Direction d) { return Move(pt, (int)d); }
  public static Point Move(Point pt, int d)
  { if(d<0) { d=d%8; if(d!=0) d+=8; }
    else if(d>7) d = d%8;
    pt.Offset(DirMap[d].X, DirMap[d].Y);
    return pt;
  }

  public static int NdN(int ndice, int nsides) // dice range from 1 to nsides, not 0 to nsides-1
  { int val=0;
    while(ndice-->0) { val += Random.Next(nsides)+1; }
    return val;
  }

  public static Direction PointToDir(Point off)
  { for(int i=0; i<8; i++) if(DirMap[i]==off) return (Direction)i;
    return Direction.Invalid;
  }

  public static bool OneIn(int n) { return Random.Next(n)==0; }

  public static int Rand(int min, int max) { return Random.Next(min, max+1); }
  public static int Rand(int max) { return Random.Next(max); }
  
  public static void RandomizeNames(string[] names)
  { for(int i=0; i<names.Length; i++)
    { int j = Global.Rand(names.Length);
      string t = names[i]; names[i] = names[j]; names[j] = t;
    }
  }

  // bouncing is incompatible with stopAtDest
  public static TraceResult TraceLine(Point start, Point dest, int maxDist, bool stopAtDest,
                                      LinePoint func, object context)
  { int dx=dest.X-start.X, dy=dest.Y-start.Y, xi=Math.Sign(dx), yi=Math.Sign(dy), r, ru, p, dist=0;
    Point op=start;
    TraceAction ta;
    if(dx<0) dx=-dx;
    if(dy<0) dy=-dy;
    if(dx>=dy)
    { r=dy*2; ru=r-dx*2; p=r-dx;
      while(true)
      { if(p>0) { start.Y+=yi; p+=ru; }
        else p+=r;
        start.X+=xi; dx--;
        ta = func(start, context);
        if(ta==TraceAction.Stop || maxDist!=-1 && ++dist>=maxDist || stopAtDest && dx<0)
          return new TraceResult(start, op);
        if(ta==TraceAction.Go) op=start;
        if((ta&TraceAction.HBounce)!=0) xi=-xi;
        if((ta&TraceAction.VBounce)!=0) { yi=-yi; start.Y=op.Y; }
      }
    }
    else
    { r=dx*2; ru=r-dy*2; p=r-dy;
      while(true)
      { if(p>0) { start.X+=xi; p+=ru; }
        else p+=r;
        start.Y+=yi; dy--;
        ta = func(start, context);
        if(ta==TraceAction.Stop || maxDist!=-1 && ++dist>=maxDist || stopAtDest && dy<0)
          return new TraceResult(start, op);
        if(ta==TraceAction.Go) op=start;
        if((ta&TraceAction.HBounce)!=0) { xi=-xi; start.X=op.X; }
        if((ta&TraceAction.VBounce)!=0) yi=-yi;
      }
    }
  }

  public static void Deserialize(System.IO.Stream stream, IFormatter formatter)
  { nextID = (ulong)formatter.Deserialize(stream);
  }
  public static void Serialize(System.IO.Stream stream, IFormatter formatter)
  { formatter.Serialize(stream, nextID);
  }

  public static readonly Point[] DirMap = new Point[8]
  { new Point(0, -1), new Point(1, -1), new Point(1, 0),  new Point(1, 1),
    new Point(0, 1),  new Point(-1, 1), new Point(-1, 0), new Point(-1, -1)
  };
  
  public static System.Collections.Hashtable ObjHash;

  static Random Random = new Random();
  static ulong nextID=1;
}

} // namespace Chrono
