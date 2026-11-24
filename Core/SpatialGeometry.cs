using System.Numerics;
namespace SyntacticWizardry.Core;

// All geometry is discrete occupancy. No rendering or game API dependencies.
public static class SpatialGeometry {
 public static Vector3 Unit(Vector3 v,Vector3 fallback)=>v.LengthSquared()>1e-8f?Vector3.Normalize(v):fallback;
 public static Voxel CellAt(Vector3 p)=>new((int)Math.Floor(p.X),(int)Math.Floor(p.Y),(int)Math.Floor(p.Z));
 public static Vector3 Center(Voxel v)=>new(v.X+.5f,v.Y+.5f,v.Z+.5f);
 public static (Vector3 Right,Vector3 Up,Vector3 Forward) Frame(Vector3 forward,Vector3 hint){
  var f=Unit(forward,Vector3.UnitZ);var u=Unit(hint,Vector3.UnitY);
  u-=f*Vector3.Dot(u,f);
  if(u.LengthSquared()<1e-8f){u=Math.Abs(f.Y)>.9f?-Vector3.UnitZ:Vector3.UnitY;u-=f*Vector3.Dot(u,f);}
  u=Unit(u,Vector3.UnitY);var r=Unit(Vector3.Cross(u,f),Vector3.UnitX);u=Unit(Vector3.Cross(f,r),Vector3.UnitY);
  return(r,u,f);
 }
 public static HashSet<Voxel> Sphere(Vector3 origin,int r,int h,int mode){
  var c=CellAt(origin);var result=new HashSet<Voxel>();long rr=r*r,hh=h*h;
  for(int y=-h;y<=h;y++){if(mode==1&&y<0||mode==2&&y>0)continue;
   for(int z=-r;z<=r;z++)for(int x=-r;x<=r;x++)if((x*x+z*z)*hh+y*y*rr<=rr*hh)result.Add(new(c.X+x,c.Y+y,c.Z+z));}
  return result;
 }
 public static HashSet<Voxel> Box(Vector3 origin,Vector3 direction,Vector3 hint,int w,int h,int d){
  var(r,u,f)=Frame(direction,hint);var c=Center(CellAt(origin))+r*(w%2==0?.5f:0)+u*(h%2==0?.5f:0)+f*(d%2==0?.5f:0);
  var extent=Vector3.Abs(r)*(w*.5f)+Vector3.Abs(u)*(h*.5f)+Vector3.Abs(f)*(d*.5f);
  var lo=CellAt(c-extent-Vector3.One);var hi=CellAt(c+extent+Vector3.One);var result=new HashSet<Voxel>();
  for(int y=lo.Y;y<=hi.Y;y++)for(int z=lo.Z;z<=hi.Z;z++)for(int x=lo.X;x<=hi.X;x++){
   var v=new Voxel(x,y,z);var delta=Center(v)-c;
   if(Math.Abs(Vector3.Dot(delta,r))<w*.5f-1e-6f&&Math.Abs(Vector3.Dot(delta,u))<h*.5f-1e-6f&&Math.Abs(Vector3.Dot(delta,f))<d*.5f-1e-6f)result.Add(v);}
  return result;
 }
 public static HashSet<Voxel> Cone(Vector3 origin,Vector3 direction,int w,int h,int d){
  var f=Unit(direction,Vector3.UnitZ);var reference=Math.Abs(f.Y)>.95f?Vector3.UnitZ:Vector3.UnitY;
  var r=Vector3.Normalize(Vector3.Cross(f,reference));var u=Vector3.Normalize(Vector3.Cross(r,f));var result=new HashSet<Voxel>();
  for(int layer=0;layer<d;layer++){double scale=(layer+1)/(double)d;int lw=Math.Max(1,(int)Math.Ceiling(w*scale)),lh=Math.Max(1,(int)Math.Ceiling(h*scale));
   var c=origin+f*layer;
   for(int y=0;y<lh;y++)for(int x=0;x<lw;x++)result.Add(CellAt(c+r*(x-(lw-1)*.5f)+u*(y-(lh-1)*.5f)));}
  return result;
 }
 // Legacy planar shapes: filled single-voxel planes perpendicular to the inherited forward vector.
 public static HashSet<Voxel> Circle(Vector3 origin,Vector3 forward,Vector3 up,int radius){
  var(r,u,_)=Frame(forward,up);var c=Center(CellAt(origin));var result=new HashSet<Voxel>();
  for(int y=-radius;y<=radius;y++)for(int x=-radius;x<=radius;x++)if(x*x+y*y<=radius*radius)result.Add(CellAt(c+r*x+u*y));return result;
 }
 public static HashSet<Voxel> Triangle(Vector3 origin,Vector3 forward,Vector3 up,int width,int height){
  var(r,u,_)=Frame(forward,up);var c=Center(CellAt(origin));var result=new HashSet<Voxel>();
  for(int y=0;y<height;y++){int count=height==1?width:1+(int)Math.Round((width-1)*(height-1-y)/(double)(height-1),MidpointRounding.AwayFromZero);
   for(int x=0;x<count;x++)result.Add(CellAt(c+r*(x-(count-1)*.5f)+u*y));}return result;
 }
}
public abstract class AreaDefinition:Definition {
 public override ComponentKind Kind=>ComponentKind.Shape;
 protected static readonly Property Width=new("width",1,10,1),Height=new("height",1,10,1),Depth=new("depth",1,10,1),Radius=new("radius",1,10,1);
 protected abstract HashSet<Voxel> Occupancy(Cell c,ShapeResolution frame);
 protected virtual ShapeResolution FrameFor(ExecutionContext c){
  var p=c.Parent;
  if(c.Cell.Row==0){var yaw=SpatialGeometry.Unit(new Vector3(p.Forward.X,0,p.Forward.Z),Vector3.UnitZ);p=p with{Origin=p.Origin+yaw};}
  return p;
 }
 public override ShapeResolution? Resolve(ExecutionContext c){var frame=FrameFor(c);return frame with{Voxels=Occupancy(c.Cell,frame),DirectEntity=null};}
 public override float Cost(Cell c)=>Occupancy(c,new ShapeResolution(new Vector3(.5f),Vector3.UnitZ,Vector3.UnitY,null,new HashSet<Voxel>(),null)).Count;
}
public sealed class SphereDefinition:AreaDefinition {
 public override string Id=>"sphere";
 public override IReadOnlyList<Property> Properties{get;}=new[]{Radius,Height,new Property("sphere_mode",0,2,0,new[]{"Sphere","Upper","Lower"})};
 protected override HashSet<Voxel> Occupancy(Cell c,ShapeResolution p)=>SpatialGeometry.Sphere(p.Origin,Value(c,"radius"),Value(c,"height"),Value(c,"sphere_mode"));
}
public sealed class BoxDefinition:AreaDefinition {
 public override string Id=>"box";
 public override IReadOnlyList<Property> Properties{get;}=new[]{Width,Height,Depth};
 protected override ShapeResolution FrameFor(ExecutionContext c){var p=base.FrameFor(c);var f=p.SurfaceNormal??SpatialGeometry.Unit(new Vector3(p.Forward.X,0,p.Forward.Z),Vector3.UnitZ);return p with{Forward=f};}
 protected override HashSet<Voxel> Occupancy(Cell c,ShapeResolution p)=>SpatialGeometry.Box(p.Origin,p.Forward,p.Up,Value(c,"width"),Value(c,"height"),Value(c,"depth"));
}
public sealed class ConeDefinition:AreaDefinition {
 public override string Id=>"cone";
 public override IReadOnlyList<Property> Properties{get;}=new[]{Depth,Height,Width};
 protected override HashSet<Voxel> Occupancy(Cell c,ShapeResolution p)=>SpatialGeometry.Cone(p.Origin,p.SurfaceNormal??p.Forward,Value(c,"width"),Value(c,"height"),Value(c,"depth"));
}
public sealed class CircleDefinition:AreaDefinition {
 public override string Id=>"circle";
 public override IReadOnlyList<Property> Properties{get;}=new[]{Radius};
 protected override HashSet<Voxel> Occupancy(Cell c,ShapeResolution p)=>SpatialGeometry.Circle(p.Origin,p.Forward,p.Up,Value(c,"radius"));
}
public sealed class TriangleDefinition:AreaDefinition {
 public override string Id=>"triangle";
 public override IReadOnlyList<Property> Properties{get;}=new[]{Width,Height};
 protected override HashSet<Voxel> Occupancy(Cell c,ShapeResolution p)=>SpatialGeometry.Triangle(p.Origin,p.Forward,p.Up,Value(c,"width"),Value(c,"height"));
}
public sealed class FloatingDefinition:Definition {
 public override string Id=>"floating";public override ComponentKind Kind=>ComponentKind.Shape;
 public override IReadOnlyList<Property> Properties{get;}=new[]{new Property("distance",1,32,8)};
 public override float Cost(Cell c)=>3;
 public override ShapeResolution? Resolve(ExecutionContext c){
  var p=c.Parent;var f=SpatialGeometry.Unit(p.Forward,Vector3.UnitZ);var origin=p.Origin+f*Value(c.Cell,"distance");
  // Floating is intentionally a free point in space; it does not perform a raycast.
  return new(origin,f,p.Up,null,new HashSet<Voxel>{SpatialGeometry.CellAt(origin)},null,VectorPolicy.Radial);
 }
}
