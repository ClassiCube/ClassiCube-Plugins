using ClassicalSharp;
using ClassicalSharp.Entities;
using OpenTK;
using System;

namespace OffsetCameraPlugin {

	public class GoodlyayCamera : PerspectiveCamera {
		public GoodlyayCamera( Game window ) : base( window ) {
			window.AddScheduledTask(1.0 / 60, TickCallback);
		}

		public override bool IsThirdPerson { get { return true; } }
		
		float dist = 3;
		public override bool Zoom( float deltaPrecise ) {
			dist = Math.Max( dist - deltaPrecise, 2 );
			return true;
		}

		public override void GetView( out Matrix4 m ) {
			Vector3 eyePos = player.EyePosition;
			eyePos.Y += bobbingVer;
			Vector3 camPos = game.CurrentCameraPos;
			
			Matrix4 lookAt;
			Matrix4.LookAt( camPos, eyePos, Vector3.UnitY, out lookAt );
			Matrix4.Mult( out m, ref lookAt, ref tiltM );
		}
		
		public override Vector2 GetCameraOrientation() {
			return new Vector2( player.HeadYRadians, player.HeadXRadians );
		}
		
		int ticks = 0;
		public override void UpdateMouse() { }
		
		void TickCallback(ScheduledTask task) {
			ticks++;
			// 3 camera ticks = 1 physics tick
			if (ticks == 3) { last = cur; ticks = 0; }
		}
		
		float last = 0, cur = 0;
		public override Vector3 GetCameraPos( float t ) {
			CalcViewBobbing( t, dist );
			Vector3 eyePos = player.EyePosition;
			eyePos.Y += bobbingVer;
			
			game.LocalPlayer.HeadX = 0;
			Vector3 vel = game.LocalPlayer.Velocity;
			if (Math.Abs(vel.X) > 0.01f || Math.Abs(vel.Z) > 0.01f)
				cur = (float)Math.Atan2(vel.X, -vel.Z) * Utils.Rad2Deg;
			game.LocalPlayer.HeadY = Utils.LerpAngle( last, cur, t );
			game.LocalPlayer.RotY = game.LocalPlayer.HeadY;
			Console.WriteLine( t);
			
			Vector3 dir = new Vector3( -1f, 0.99f, -1f );
			Picking.ClipCameraPos( game, eyePos, dir, dist, game.CameraClipPos );
			return game.CameraClipPos.Intersect;
		}
	}
}
