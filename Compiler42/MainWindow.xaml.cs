
// OpenTK公式チュートリアル
// https://opentk.net/learn/index.html

// WPFへの導入
// https://qiita.com/sekky0816/items/91bc38b0242494ba9954

// タイマ
// https://www.atmarkit.co.jp/ait/articles/1812/12/news014.html

// キーボード取得（リアルタイム）
// https://www.ipentec.com/document/csharp-check-keboard-key-press-state

// ラベルをHostより上に描画
// https://chitoku.jp/programming/draw-wpf-controls-over-hwndhosts
// https://stackoverflow.com/questions/1600218/how-can-i-move-a-wpf-popup-when-its-anchor-element-moves

// メニュー
// https://araramistudio.jimdo.com/2019/11/05/c-%E3%81%AEwpf%E3%81%A7%E3%83%A1%E3%83%8B%E3%83%A5%E3%83%BC%E3%82%92%E4%BD%9C%E6%88%90%E3%81%99%E3%82%8B/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using System.Windows.Threading;
// using System.Windows.Forms;

using System.Drawing;
using System.Windows.Media;

namespace Compiler42 {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		static GraphicsMode mode = new GraphicsMode(
												 GraphicsMode.Default.ColorFormat,
												 GraphicsMode.Default.Depth,
												 8,//GraphicsMode.Default.Stencil,
												 8,//GraphicsMode.Default.Samples,
												 GraphicsMode.Default.AccumulatorFormat,
												 GraphicsMode.Default.Buffers,
												 GraphicsMode.Default.Stereo
												 );

		//コンテキストの作成
		GLControl glControl = new GLControl(mode);

		public MainWindow() {
			InitializeComponent();

			SetupTimer();
			SetupTimer2();

			//イベントの追加
			glControl.Load += glControl_Load;
			glControl.Paint += glControl_Paint;
			glControl.Resize += glControl_Resize;
			//ホストの子に設定
			glHost.Child = glControl;

		}

		float rx, ry;

		float minDistance = 0.01f;
		float MaxDistance = 1000f;

		bool CameraRotate = false;
		Vector2 lastPos;

		Matrix4 proj;
		Matrix4 look;

		float speed = 0.1f;
		float Sensitivity = 0.2f;

		float yaw = -90f;   // ヨーとfrontベクトルの初期値は一致させる必要がある
		float pitch = 0f;

		Vector3 position = new Vector3(0.0f, 0.0f, 3.0f);
		Vector3 front = new Vector3(0.0f, 0.0f, -1.0f);
		Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);

		double dpiX, dpiY;
		int CenterX, CenterY;
		int MonitorX, MonitorY;

		int fpsCount;

		private void glControl_Load(object sender, EventArgs e) {

			GL.ClearColor(Color4.Black);

			// ビューポートの設定
			GL.Viewport(0, 0, glControl.Width, glControl.Height);

			// 視体積の設定
			GL.MatrixMode(MatrixMode.Projection);
			proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), glControl.AspectRatio, minDistance, MaxDistance);
			GL.LoadMatrix(ref proj);

			// 視界の設定
			GL.MatrixMode(MatrixMode.Modelview);

			// デプスバッファの使用
			GL.Enable(EnableCap.DepthTest);

			// 光源の使用
			GL.Enable(EnableCap.Lighting);

			//			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
		}

		private void glControl_Resize(object sender, EventArgs e) {

			GL.Viewport(0, 0, glControl.Width, glControl.Height);

			GL.MatrixMode(MatrixMode.Projection);
			proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), glControl.AspectRatio, minDistance, MaxDistance);
			GL.LoadMatrix(ref proj);
			GL.MatrixMode(MatrixMode.Modelview);

		}

		private void glControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e) {

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			GL.Material(MaterialFace.Front, MaterialParameter.Emission, Color4.Blue);
			tube(2, 0.1f, 0.1f);

			GL.Material(MaterialFace.Front, MaterialParameter.Emission, Color4.Green);
			square(1.5f);

			glControl.SwapBuffers();

		}

		private void SetInitSight() {

			look = Matrix4.LookAt(position, position + front, up);
			GL.LoadMatrix(ref look);
//			glControl.Refresh();

			glControl.Invalidate();
		}

		void tube(float length, float radius1, float radius2) {

			GL.PushMatrix();
			GL.Begin(PrimitiveType.TriangleStrip);
			GL.Normal3(Vector3.One);
			for (int deg = 0; deg <= 360; deg = deg + 30) {
				rx = (float)Math.Cos((float)Math.PI * deg / 180);
				ry = (float)Math.Sin((float)Math.PI * deg / 180);
				GL.Vertex3(rx * radius2, ry * radius2, length / 2);
				GL.Vertex3(rx * radius1, ry * radius1, -length / 2);

			}
			GL.End();
			GL.PopMatrix();

		}

		void square(float length) {

			GL.PushMatrix();
			GL.Begin(PrimitiveType.TriangleStrip);
			GL.Normal3(Vector3.One);

			GL.Vertex2(length, length);
			GL.Vertex2(-1 * length, length);
			GL.Vertex2(length, -1 * length);
			GL.Vertex2(-1 * length, -1 * length);

			GL.End();
			GL.PopMatrix();

		}

		private void Window_LocationChanged(object sender, EventArgs e) {

			Popup_Move();

		}

		private void Window_SizeChanged(object sender, EventArgs e) {

			Popup_Move();

		}

		private void Window_MouseMove(object sender, MouseEventArgs e) {        // WindowsFormHostのプロパティから共通→IsEnabledをOFFにすること
																				// チェックが入ったままだと描画領域上でマウスイベントが使えない
			if (CameraRotate) {

				if (yaw > 180f) {
					yaw -= 360f;
				} else if (yaw < -180f) {
					yaw += 360f;
				} else {
					yaw += (System.Windows.Forms.Cursor.Position.X - lastPos[0]) * Sensitivity;
				}

				if (pitch > 89.0f) {
					pitch = 89.0f;
				} else if (pitch < -89.0f) {
					pitch = -89.0f;
				} else {
					pitch -= (System.Windows.Forms.Cursor.Position.Y - lastPos[1]) * Sensitivity;
				}

				lastPos[0] = System.Windows.Forms.Cursor.Position.X;
				lastPos[1] = System.Windows.Forms.Cursor.Position.Y;

				front[0] = (float)Math.Cos(MathHelper.DegreesToRadians(pitch)) * (float)Math.Cos(MathHelper.DegreesToRadians(yaw));
				front[1] = (float)Math.Sin(MathHelper.DegreesToRadians(pitch));
				front[2] = (float)Math.Cos(MathHelper.DegreesToRadians(pitch)) * (float)Math.Sin(MathHelper.DegreesToRadians(yaw));

				front = Vector3.Normalize(front);

				//				if (Math.Abs(lastPos[0] - CenterX) > 100 || Math.Abs(lastPos[1] - CenterY) > 100) {
				//					SetCursorPos(CenterX, CenterY);
				//				}

			}
		}

		private void Window_PreviewKeyDown(Object sender, KeyEventArgs e) {

			switch (e.Key) {
				case Key.Escape:
					Close();
					break;
				case Key.Space:
					CameraRotate = !CameraRotate;
					if(this.IsActive){
						if (CameraRotate) {

							//							SetCursorPos(CenterX, CenterY);

							lastPos[0] = System.Windows.Forms.Cursor.Position.X;
							lastPos[1] = System.Windows.Forms.Cursor.Position.Y;

							CameraLabel.Foreground = new SolidColorBrush(Colors.Magenta);

						}else{

							CameraLabel.Foreground = new SolidColorBrush(Colors.Transparent);

						}

					}

					break;

			}

		}

		private void Popup_Move(){

			fpsPop.HorizontalOffset = -1 * this.ActualWidth / 2 + 50;
			fpsPop.VerticalOffset = -1 * this.ActualHeight / 2 + 50 + 15.96;

			XYZPop.HorizontalOffset = -1 * this.ActualWidth / 2 + 200;
			XYZPop.VerticalOffset = -1 * this.ActualHeight / 2 + 50 + 15.96;

			CameraPop.HorizontalOffset = this.ActualWidth / 2 - 100;
			CameraPop.VerticalOffset = this.ActualHeight / 2 - 80 + 15.96;

			fpsPop.HorizontalOffset += 0.001;		// オフセットを動かすことで再描画が可能
			fpsPop.HorizontalOffset -= 0.001;
			XYZPop.HorizontalOffset += 0.001;
			XYZPop.HorizontalOffset -= 0.001;
			CameraPop.HorizontalOffset += 0.001;
			CameraPop.HorizontalOffset -= 0.001;

		}

		private void SetupTimer() {

			var timer = new DispatcherTimer(DispatcherPriority.Normal) {
				Interval = TimeSpan.FromSeconds(0.001),
			};

			timer.Tick += (s, e) => {

				if (this.IsActive) {

					if (Keyboard.IsKeyDown(Key.A)) {
						position -= Vector3.Normalize(Vector3.Cross(front, up)) * speed;
					}
					if (Keyboard.IsKeyDown(Key.D)) {
						position += Vector3.Normalize(Vector3.Cross(front, up)) * speed;
					}
					if (Keyboard.IsKeyDown(Key.W)) {
						position += front * speed;
					}
					if (Keyboard.IsKeyDown(Key.S)) {
						position -= front * speed;
					}
					if (Keyboard.IsKeyDown(Key.Up)) {
						position += up * speed;
					}
					if (Keyboard.IsKeyDown(Key.Down)) {
						position -= up * speed;
					}

					SetInitSight();

				}

				fpsCount++;

			};

			timer.Start();
			this.Closing += (s, e) => timer.Stop();
		}

		private void SetupTimer2() {

			var timer2 = new DispatcherTimer(DispatcherPriority.Normal) {
				Interval = TimeSpan.FromSeconds(0.5),
			};

			timer2.Tick += (s, e) => {

				fpsLabel.Content = "FPS " + fpsCount * 2;
				XYZLabel.Content = "X: " + string.Format("{0:f2}", position[0]) + "  Y: " + string.Format("{0:f2}", position[1]) + "  Z: " + string.Format("{0:f2}", position[2]);
				fpsCount = 0;
			};

			timer2.Start();
			this.Closing += (s, e) => timer2.Stop();

		}

	}

}