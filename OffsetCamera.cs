	public class GoodlyayCamera : PerspectiveCamera {
		public GoodlyayCamera( Game window ) : base( window ) { }
		public override bool IsThirdPerson { get { return true; } }
		
		float dist = 3;
		public override bool DoZoom( float deltaPrecise ) {
			dist = Math.Max( dist - deltaPrecise, 2 );
			return true;
		}
		
		public override Matrix4 GetView() {
			Vector3 eyePos = player.EyePosition;
			eyePos.Y += bobbingVer;			
			Vector3 cameraPos = game.CurrentCameraPos;
			return Matrix4.LookAt( cameraPos, eyePos, Vector3.UnitY ) * tiltM;
		}
		
		public override Vector2 GetCameraOrientation() {
			return new Vector2( player.HeadYawRadians, player.PitchRadians );	
		}
		
		int ticks = 0;
		public override void Tick(double elapsed) {
			base.Tick(elapsed);
			ticks++;
			// 3 camera ticks = 1 physics tick
			if (ticks == 3) { last = cur; ticks = 0; }
		}
		
		float last = 0, cur = 0;		
		public override Vector3 GetCameraPos( float t ) {
			CalcViewBobbing( t, dist );
			Vector3 eyePos = player.EyePosition;
			eyePos.Y += bobbingVer;
			
			game.LocalPlayer.PitchDegrees = 0;
			Vector3 vel = game.LocalPlayer.Velocity;
			if (Math.Abs(vel.X) > 0.01f || Math.Abs(vel.Z) > 0.01f)
				cur = (float)Math.Atan2(vel.X, -vel.Z) * Utils.Rad2Deg;
			game.LocalPlayer.YawDegrees = Utils.LerpAngle( last, cur, t );
			game.LocalPlayer.HeadYawDegrees = game.LocalPlayer.YawDegrees;
			Console.WriteLine( t);
			
			Vector3 dir = new Vector3( -1f, 0.99f, -1f );
			Picking.ClipCameraPos( game, eyePos, dir, dist, game.CameraClipPos );
			return game.CameraClipPos.Intersect;
		}
	}
