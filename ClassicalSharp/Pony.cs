using System;
using ClassicalSharp;
using ClassicalSharp.Entities;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Map;
using ClassicalSharp.Model;
using ClassicalSharp.Physics;
using OpenTK;

namespace Pony {

	public sealed class Core : Plugin {
		
        public int APIVersion { get { return 2; } }
		
		public void Dispose() { }
		
		public void Init(Game game) {
			game.ModelCache.RegisterTextures("pony.png");
			
			game.ModelCache.Register("pony", "pony.png", new PonyModel(game));
			
			game.ModelCache.Register("ponysit", "pony.png", new PonySitModel(game));
			game.ModelCache.Register("ponysitting", "pony.png", new PonySitModel(game));
			
			game.ModelCache.Register("tallpony", "pony.png", new TallPonyModel(game));

			
			// Recreate the modelcache VB to be bigger
			game.Graphics.DeleteVb(ref game.ModelCache.vb);
			game.ModelCache.vertices = new VertexP3fT2fC4b[24 * 20];
			game.ModelCache.vb = game.Graphics.CreateDynamicVb(VertexFormat.P3fT2fC4b,
			                                                   game.ModelCache.vertices.Length);
			game.Server.AppName += " + Ponies v2.1";
		}
		
		public void Ready(Game game) { }
		
		public void Reset(Game game) { }
		
		public void OnNewMap(Game game) { }
		
		public void OnNewMapLoaded(Game game) { }
	}
	
	public class PonyModel : IModel {
		
        public float headTilt;
        public float hoofOffset;
        
		public PonyModel(Game window) : base(window) {
			UsesHumanSkin = true;
        }
		
		protected virtual void SetHoofOffset() {
			hoofOffset = 0;
		}
		
		public override void CreateParts() {
		    SetHoofOffset();
			vertices = new ModelVertex[boxVertices * 22]; //20 so we have enough here for Pony2. Was 17
			RightWing2 = BuildWing(MakeBoxBounds(0, 0, 0, 13, 1, 8)
			                      .SetModelBounds(-17, 12.5f, -8, -4, 13.5f, 0)
			                      .TexOrigin(23, 39)
			                      .RotOrigin(-4, 13, -4));
			LeftWing2 = BuildWing(MakeBoxBounds(0, 0, 0, 13, 1, 8)
			                     .SetModelBounds(17, 12.5f, -8, 4, 13.5f, 0)
			                     .TexOrigin(23, 39)
			                     .RotOrigin(4, 13, -4));
			LeftWing = BuildBox(MakeBoxBounds(0, 0, 0, 1, 4, 7)
			                    .SetModelBounds(-5, 11, -5, -4, 15, 2)
			                    .TexOrigin(41, 17)
			                    .RotOrigin(-4, 18, -5));
			RightWing = BuildBox(MakeBoxBounds(0, 0, 0, 1, 4, 7)
			                     .SetModelBounds(5, 11, -5, 4, 15, 2)
			                     .TexOrigin(41, 17)
			                     .RotOrigin(-4, 18, -5));
			
			Head = BuildBox(MakeBoxBounds(0, 0, 0, 8, 8, 8)
			                .SetModelBounds(-4, 18, -10, 4, 26, -2)
			                .TexOrigin(0, 0)
			                .RotOrigin(0, 18, -5));
			Hat = BuildBox(MakeBoxBounds(0, 0, 0, 8, 8, 8)
			               .SetModelBounds(-4, 18, -10, 4, 26, -2)
			               .TexOrigin(32, 0)
			               .RotOrigin(0, 18, -5).Expand(0.5F));
			Snout = BuildBox(MakeBoxBounds(0, 0, 0, 3, 2, 1)
			                 .SetModelBounds(-1.5F, 18, -11, 1.5F, 20, -10)
			                 .TexOrigin(0, 37)
			                 .RotOrigin(0, 18, -5));
			Horn = BuildBox(MakeBoxBounds(0, 0, 0, 1, 5, 1)
			                .SetModelBounds(-0.5F, 26, -7, 0.5F, 31, -6)
			                .TexOrigin(24, 0)
			                .RotOrigin(0, 18, -5));
			LeftEar = BuildBox(MakeBoxBounds(0, 0, 0, 3, 4, 1)
			                   .SetModelBounds(-4.5F, 24, -5, -1.5F, 28, -4)
			                   .TexOrigin(0, 0)
			                   .RotOrigin(0, 18, -5));
			RightEar = BuildBox(MakeBoxBounds(0, 0, 0, 3, 4, 1)
			                    .SetModelBounds(4.5F, 24, -5, 1.5F, 28, -4)
			                    .TexOrigin(0, 0)
			                    .RotOrigin(0, 18, -5));
			
			
			Torso = BuildBox(MakeBoxBounds(0, 0, 0, 8, 8, 12)
			                 .SetModelBounds(-4, 8, -6, 4, 16, 6)
			                 .TexOrigin(0, 16)
			                 .RotOrigin(0, 12, -2));
			
			
			Tail = RightQuad(MakeBoxBounds(0, 0, 0, 2, 16, 16)
			                .SetModelBounds(-2, 3, 6, 0, 19, 22)
			                .TexOrigin(41, 32)
			                .RotOrigin(-1, 14, 6));
			Neck = BuildBox(MakeBoxBounds(0, 0, 0, 4, 7, 5)
			                .SetModelBounds(-2, 12.5F, -7, 2, 19.5F, -2)
			                .TexOrigin(5, 36)
			                .RotOrigin(-4, 18, -5));
			
			LeftLegFront = BuildBox(MakeBoxBounds(0, 0, 0, 3, 8, 3)
			                        .SetModelBounds(0.5F, 0+hoofOffset, -5.5F, 3.5F, 8, -2.5F)
			                        .TexOrigin(28, 16)
			                        .RotOrigin(-4, 8, -4));
			RightLegFront = BuildBox(MakeBoxBounds(0, 0, 0, 3, 8, 3)
			                         .SetModelBounds(-0.5F, 0+hoofOffset, -5.5F, -3.5F, 8, -2.5F)
			                         .TexOrigin(28, 16)
			                         .RotOrigin(-4, 8, -4));
			
			LeftLegBack = BuildBox(MakeBoxBounds(0, 0, 0, 3, 8, 3)
			                       .SetModelBounds(1, 0+hoofOffset, 4, 4, 8, 7)
			                       .TexOrigin(0, 16)
			                       .RotOrigin(-3, 8, 5));
			RightLegBack = BuildBox(MakeBoxBounds(0, 0, 0, 3, 8, 3)
			                        .SetModelBounds(-1, 0+hoofOffset, 4, -4, 8, 7)
			                        .TexOrigin(0, 16)
			                        .RotOrigin(3, 8, 5));
			
			
			
			
			float offsetX = -2; float offsetY = 17; float offsetZ = -15;
			Hair1 = RightQuad(MakeBoxBounds(0, 0, 0, 2, 16, 16) //middle
			                 .SetModelBounds(offsetX, offsetY, offsetZ, 2+offsetX, 16+offsetY, 16+offsetZ)
			                 .TexOrigin(41, 49)
			                 .RotOrigin(0, 18, -5));
			
			offsetX = 2; offsetY = 12; offsetZ = -14;
			Hair2 = RightQuad(MakeBoxBounds(0, 0, 0, 2, 16, 16) //right side
			                 .SetModelBounds(offsetX, offsetY, offsetZ, 2+offsetX, 16+offsetY, 16+offsetZ)
			                 .TexOrigin(5, 49)
			                 .RotOrigin(0, 18, -5).Expand(0.5f));
			
			offsetX = -4; offsetY = 12; offsetZ = -14;
			Hair3 = LeftQuad(MakeBoxBounds(0, 0, 0, 2, 16, 16) //left side
			                 .SetModelBounds(offsetX, offsetY, offsetZ, 2+offsetX, 16+offsetY, 16+offsetZ)
			                 .TexOrigin(5, 66)
			                 .RotOrigin(0, 18, -5).Expand(0.5f));
		}
        
		protected ModelPart LeftQuad(BoxDesc desc) {
            //SidesW = SizeX
            //BodyH = SizeY
            //BodyW = SizeZ
			int sidesW = desc.SizeZ, bodyW = desc.SizeX, bodyH = desc.SizeY;
			float x1 = desc.X1, y1 = desc.Y1, z1 = desc.Z1;
			float x2 = desc.X2, y2 = desc.Y2, z2 = desc.Z2;
			int x = desc.TexX, y = desc.TexY;


			
						ModelBuilder.XQuad(this, x + sidesW + bodyW, y + sidesW, sidesW, bodyH, z2, z1, y1, y2, x1, true); // left for real
			return new ModelPart(index - 1 * 4, 1 * 4, desc.RotX, desc.RotY, desc.RotZ);
		}
		
		protected ModelPart RightQuad(BoxDesc desc) {
			int sidesW = desc.SizeZ, bodyW = desc.SizeX, bodyH = desc.SizeY;
			float x1 = desc.X1, y1 = desc.Y1, z1 = desc.Z1;
			float x2 = desc.X2, y2 = desc.Y2, z2 = desc.Z2;
			int x = desc.TexX, y = desc.TexY;


			
						ModelBuilder.XQuad(this, x, y + sidesW, sidesW, bodyH, z1, z2, y1, y2, x2, true); // right for real
			return new ModelPart(index - 1 * 4, 1 * 4, desc.RotX, desc.RotY, desc.RotZ);
        }
        
		protected ModelPart BuildWing(BoxDesc desc) {
			int sidesW = desc.SizeZ, bodyW = desc.SizeX, bodyH = desc.SizeY;
			float x1 = desc.X1, y1 = desc.Y1, z1 = desc.Z1;
			float x2 = desc.X2, y2 = desc.Y2, z2 = desc.Z2;
			int x = desc.TexX, y = desc.TexY;
			
			ModelBuilder.YQuad(this, x + sidesW, y, bodyW, sidesW, x1, x2, z2, z1, y2, true); // top
			ModelBuilder.YQuad(this, x + sidesW + bodyW, y, bodyW, sidesW, x2, x1, z2, z1, y1, false); // bottom
			return new ModelPart(index - 2 * 4, 2 * 4, desc.RotX, desc.RotY, desc.RotZ);
		}
		
		public override float NameYOffset { get { return 28/16f; } }
		
		public override float GetEyeY(Entity entity) { return 21/16f; }
		
		public override Vector3 CollisionSize {
			get { return new Vector3(8/16f + 0.6f/16f, 26.1f/16f, 8/16f + 0.6f/16f); }
		}
		
		public override AABB PickingBounds {
			get { return new AABB(-5/16f, 0, -14/16f, 5/16f, 28/16f, 9/16f); }
		}
		
		protected void SetUVScale() {
			vScale = 1 /128f; uScale = 1 /128f;
		}
		
		public override void DrawModel(Entity p) {

			cols[1] = cols[2] = cols[3] = cols[4] = cols[5] = PackedCol.Scale(cols[0], PackedCol.ShadeZ);
			

			ApplyTexture(p);
			SetUVScale();
			
			headTilt = -p.HeadX;
			if(headTilt >= -180)
				headTilt *= 0.5f;
			else
				headTilt = -180 + headTilt * 0.5f; // -360 + (360 + headTilt) * 0.5f
			
			game.Graphics.AlphaTest = false;
			DrawRotate(headTilt * Utils.Deg2Rad, 0, 0, Head, true);
			DrawPart(Torso);
			UpdateVB();
			game.Graphics.AlphaTest = true;
			index = 0;
			
			if (NotAllBelowAir(p)) {
				DrawPart(LeftWing);
				DrawPart(RightWing);
			} else {
				const float legMax = 80 * Utils.Deg2Rad; // stolen from animatedcomponent
				float wingZRot = -(float)(Math.Cos(game.accumulator * 10) * legMax);
				DrawRotate(0, 0, -wingZRot, RightWing2, false);
				DrawRotate(0, 0, wingZRot, LeftWing2, false);
			}
			
			DrawRotate(headTilt * Utils.Deg2Rad, 0, 0, Hat, true);
			DrawRotate(headTilt * Utils.Deg2Rad -0.4F, 0, 0, Horn, true);
			DrawRotate(headTilt * Utils.Deg2Rad, 0, 0, LeftEar, true);
			DrawRotate(headTilt * Utils.Deg2Rad, 0, 0, RightEar, true);
			DrawRotate(headTilt * Utils.Deg2Rad, 0, 0, Snout, true);
			DrawRotate(-0.4F, 0, 0, Neck, false);
			const float tailTilt = 0.05F;
			const float tailRoll = 0.1F;
			
			PackedCol tempCol = cols[0];
			cols[0] = PackedCol.Scale(cols[0], PackedCol.ShadeZ);
			DrawRotate(0, 0, 0, Tail, false);
			DrawRotate(0,  tailTilt, -tailRoll, Tail, false);
			DrawRotate(0, -tailTilt, tailRoll, Tail, false);
			cols[0] = tempCol;
			
			if (!NotAllBelowAir(p)) {
				DrawPart(LeftLegFront);
				DrawPart(RightLegFront);
				DrawPart(LeftLegBack);
				DrawPart(RightLegBack);
			} else {
				DrawRotate(p.anim.leftLegX * 0.5F, 0, 0, LeftLegFront, false);
				DrawRotate(p.anim.rightLegX * 0.5F, 0, 0, RightLegFront, false);
				DrawRotate(p.anim.rightLegX * 0.5F, 0, 0, LeftLegBack, false);
				DrawRotate(p.anim.leftLegX * 0.5F, 0, 0, RightLegBack, false);
			}
			
			
            cols[0] = PackedCol.Scale(cols[0], PackedCol.ShadeZ);
            
			DrawRotate(headTilt * Utils.Deg2Rad, -0.2f, 0, Hair1, true); //-0.2f
			DrawRotate(headTilt * Utils.Deg2Rad, -0, 0, Hair2, true); //-0.2f
			DrawRotate(headTilt * Utils.Deg2Rad, -0, 0, Hair3, true); //-0.2f
			
			UpdateVB();
		}
		
		protected bool NotAllBelowAir(Entity p) {
			AABB bb = AABB.Make(p.Position, p.Size * 2);
			bb.Max.Y = p.Position.Y - 0.5f;
			bb.Min.Y = p.Position.Y - 1.5f;
			return p.TouchesAny(bb, (ushort b) => BlockInfo.Draw[b] != DrawType.Gas);
		}
		
		public ModelPart Head, Horn, LeftEar, RightEar, Hat, Neck, Snout, Torso, LeftWing, RightWing, Tail, LeftLegFront, RightLegFront, LeftLegBack, RightLegBack, LeftWing2, RightWing2, Hair1, Hair2, Hair3;
	}
    
    
    public class PonySitModel : PonyModel {
        
		public PonySitModel(Game window) : base(window) {
			UsesHumanSkin = true;
        }
        
        public override void CreateParts() {
            
            base.CreateParts();
            
            float frontLegOffsetY = 2f;
            float frontLegOffsetZ = -1f;
			LeftLegFront = BuildBox(MakeBoxBounds(0, 0, 0, 3, 8, 3)
			                        .SetModelBounds(0.5F, 0+hoofOffset +frontLegOffsetY, -5.5F+frontLegOffsetZ, 3.5F, 8+frontLegOffsetY, -2.5F+frontLegOffsetZ)
			                        .TexOrigin(28, 16)
			                        .RotOrigin(-4, (sbyte)(8+frontLegOffsetY), (sbyte)(-4+frontLegOffsetZ)));
			RightLegFront = BuildBox(MakeBoxBounds(0, 0, 0, 3, 8, 3)
			                         .SetModelBounds(-0.5F, 0+hoofOffset +frontLegOffsetY, -5.5F +frontLegOffsetZ, -3.5F, 8+frontLegOffsetY, -2.5F+frontLegOffsetZ)
			                         .TexOrigin(28, 16)
			                        .RotOrigin(-4, (sbyte)(8+frontLegOffsetY), (sbyte)(-4+frontLegOffsetZ)));
            
            
            float backLegOffsetX = 1f;
            
            float backLegOffsetY = -3f;
            float backLegOffsetZ = -3f;
			LeftLegBack = BuildBox(MakeBoxBounds(0, 0, 0, 3, 8, 3)
			                       .SetModelBounds(1+backLegOffsetX, 0+hoofOffset+backLegOffsetY, 4+backLegOffsetZ, 4+backLegOffsetX, 8+backLegOffsetY, 7+backLegOffsetZ)
			                       .TexOrigin(0, 16)
			                       .RotOrigin((sbyte)(3+backLegOffsetX), (sbyte)(8+backLegOffsetY), (sbyte)(5+backLegOffsetZ)));
			RightLegBack = BuildBox(MakeBoxBounds(0, 0, 0, 3, 8, 3)
			                        .SetModelBounds(-1-backLegOffsetX, 0+hoofOffset+backLegOffsetY, 4+backLegOffsetZ, -4-backLegOffsetX, 8+backLegOffsetY, 7+backLegOffsetZ)
			                        .TexOrigin(0, 16)
			                        .RotOrigin((sbyte)(-3-backLegOffsetX), (sbyte)(8+backLegOffsetY), (sbyte)(5+backLegOffsetZ)));
            
            float tailOffsetY = -6f;
            float tailOffsetZ = -0.8f;
			Tail = RightQuad(MakeBoxBounds(0, 0, 0, 2, 16, 16)
			                .SetModelBounds(-2, 3+tailOffsetY, 6+tailOffsetZ, 0, 19+tailOffsetY, 22+tailOffsetZ)
			                .TexOrigin(41, 32)
			                .RotOrigin(-1, (sbyte)(14+tailOffsetY), (sbyte)(6+tailOffsetZ)));
            
            
            
			LeftWing = BuildBox(MakeBoxBounds(0, 0, 0, 1, 4, 7)
			                    .SetModelBounds(-5, 11+tailOffsetY, -5+tailOffsetZ, -4, 15+tailOffsetY, 2+tailOffsetZ)
			                    .TexOrigin(41, 17)
			                    .RotOrigin(-1, (sbyte)(14+tailOffsetY), (sbyte)(6+tailOffsetZ)));
			RightWing = BuildBox(MakeBoxBounds(0, 0, 0, 1, 4, 7)
			                     .SetModelBounds(5, 11+tailOffsetY, -5+tailOffsetZ, 4, 15+tailOffsetY, 2+tailOffsetZ)
			                     .TexOrigin(41, 17)
			                     .RotOrigin(-1, (sbyte)(14+tailOffsetY), (sbyte)(6+tailOffsetZ)));
        }
        
		float tempRotate = 0.77f;
		float legRotY = 0;
		float legRotZ = 0.3f;
		public override void DrawModel(Entity p) {

			cols[1] = cols[2] = cols[3] = cols[4] = cols[5] = PackedCol.Scale(cols[0], PackedCol.ShadeZ);
			
			
			ApplyTexture(p);
			SetUVScale();
			
			headTilt = -p.HeadX;
			if(headTilt >= -180)
				headTilt *= 0.5f;
			else
				headTilt = -180 + headTilt * 0.5f; // -360 + (360 + headTilt) * 0.5f
			
			game.Graphics.AlphaTest = false;
			DrawRotate(headTilt * Utils.Deg2Rad, 0, 0, Head, true);
			

			DrawRotate(tempRotate, 0, 0, Torso, false); //TORSO*********************************
			//DrawPart(Torso);
			
			
			UpdateVB();
			game.Graphics.AlphaTest = true;
			index = 0;
			

			DrawRotate(tempRotate, 0, 0, LeftWing,false);
			DrawRotate(tempRotate, 0, 0, RightWing, false);
			
			DrawRotate(headTilt * Utils.Deg2Rad, 0, 0, Hat, true);
			DrawRotate(headTilt * Utils.Deg2Rad -0.4F, 0, 0, Horn, true);
			DrawRotate(headTilt * Utils.Deg2Rad, 0, 0, LeftEar, true);
			DrawRotate(headTilt * Utils.Deg2Rad, 0, 0, RightEar, true);
			DrawRotate(headTilt * Utils.Deg2Rad, 0, 0, Snout, true);
			DrawRotate(-0.4F, 0, 0, Neck, false);
			const float tailTilt = 0.05F;
			const float tailRoll = 0.1F;
			
			PackedCol tempCol = cols[0];
			cols[0] = PackedCol.Scale(cols[0], PackedCol.ShadeZ);
			DrawRotate(tempRotate, 0, 0, Tail, false);
			DrawRotate(tempRotate,  tailTilt, -tailRoll, Tail, false);
			DrawRotate(tempRotate, -tailTilt, tailRoll, Tail, false);
			cols[0] = tempCol;
			
			
			DrawRotate(0.4F, 0, 0, LeftLegFront, false);
			DrawRotate(0.4F, 0, 0, RightLegFront, false);
			
			DrawRotate(90 * Utils.Deg2Rad, legRotY, legRotZ, LeftLegBack, false);
			DrawRotate(90 * Utils.Deg2Rad, legRotY, -legRotZ, RightLegBack, false);
			
			
            cols[0] = PackedCol.Scale(cols[0], PackedCol.ShadeZ);
            
			DrawRotate(headTilt * Utils.Deg2Rad, -0.2f, 0, Hair1, true); //-0.2f
			DrawRotate(headTilt * Utils.Deg2Rad, -0, 0, Hair2, true); //-0.2f
			DrawRotate(headTilt * Utils.Deg2Rad, -0, 0, Hair3, true); //-0.2f
			
			UpdateVB();
		}
        
        const int sitOffset = -3;
        
        public override float GetEyeY(Entity entity) { return (21 + sitOffset)/16f; }
        
		protected override Matrix4 TransformMatrix(Entity p, Vector3 pos) {
			pos.Y += (sitOffset / 16f) * p.ModelScale.Y;
			return p.TransformMatrix(p.ModelScale, pos);
		}
        
    }
    
    
    public class TallPonyModel : PonyModel {
        
        int tallOffset = 4;
        
		public TallPonyModel(Game window) : base(window) {
			UsesHumanSkin = true;
        }
		
		protected override void SetHoofOffset() {
		    hoofOffset = -tallOffset;
		}
        
        public override float GetEyeY(Entity entity) { return (21 + tallOffset)/16f; }
		
		protected override Matrix4 TransformMatrix(Entity p, Vector3 pos) {
			pos.Y += (tallOffset / 16f) * p.ModelScale.Y;
			return p.TransformMatrix(p.ModelScale, pos);
		}
		
    }
 
}
