
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

// dpi
// https://slash-mochi.net/?p=3370

// 最大化時のサイズ取得
// https://araramistudio.jimdo.com/2016/11/10/c-%E3%81%A7%E7%94%BB%E9%9D%A2%E3%81%AE%E8%A7%A3%E5%83%8F%E5%BA%A6%E3%82%92%E5%8F%96%E5%BE%97%E3%81%99%E3%82%8B/

// ウィンドウの最大化判断
// https://stackoverflow.com/questions/6071372/maximize-wpf-window-on-the-current-screen

// ワイヤーフレーム・面表示
// https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glPolygonMode.xhtml

// 透視投影・平行投影
// http://wisdom.sakura.ne.jp/system/opengl/gl12.html

// Zバッファで複数viewportの干渉を防ぐ
// https://stackoverflow.com/questions/13710791/multiple-viewports-interfering

// Z-fighting対策（未使用）
// https://stackoverflow.com/questions/6892489/how-to-get-the-rid-of-the-z-fighting-problem-in-opengl

// 対数Zバッファ（長距離の描画に使うかも）
// https://www.gamasutra.com/blogs/BranoKemen/20090812/2725/Logarithmic_ZBuffer.php

// キューブマップ

// ファイル存在確認
// https://dobon.net/vb/dotnet/file/fileexists.html


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

using System.Runtime.InteropServices;

using System.IO;
using System.Xml.Serialization;

namespace Compiler42 {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		static GraphicsMode mode = new GraphicsMode(
												 GraphicsMode.Default.ColorFormat,
												 GraphicsMode.Default.Depth,				// 何bitかによってz-fightingが起こりそうだけど大丈夫？
												 8,//GraphicsMode.Default.Stencil,
												 8,//GraphicsMode.Default.Samples,
												 GraphicsMode.Default.AccumulatorFormat,
												 GraphicsMode.Default.Buffers,
												 GraphicsMode.Default.Stereo
												 );

		//コンテキストの作成
		GLControl glControl = new GLControl(mode);

		[DllImport("User32.dll")]
		private static extern bool SetCursorPos(int X, int Y);

		PresentationSource MainWindowPresentationSource;
		Matrix m;
		

		public MainWindow() {

			var ObjectOfLoadDDS = new LoadDDS();

			InitializeComponent();

			SetupTimer();
			SetupTimer2();

			glControl.Load += glControl_Load;
//			glControl.Paint += glControl_Paint;
//			glControl.Resize += glControl_Resize;
			
			glHost.Child = glControl;

			ObjectOfLoadDDS.ReadDDS("Texture/404Texture_1024.dds");

		}

		float rx, ry;

		float minDistance = 0.01f;
		float MaxDistance = 1000f;

		bool CameraRotate = false;
		Vector2 lastPos;

		Matrix4 proj;
		Matrix4 look;
		Matrix4 lookCubeMap;
		Matrix4 lookMap;

		float Speed = 0.1f;
		float Sensitivity = 0.2f;

		float ActSpeed;

		float yaw = 90f;   // ヨーとfrontベクトルの初期値は一致させる必要がある
		float pitch = 0f;

		Vector3 position = new Vector3(0.0f, 0.0f, -3.0f);
		Vector3 front = new Vector3(0.0f, 0.0f, 1.0f);
		Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);

		double dpiX, dpiY;
		int CenterX, CenterY;

		int MapViewSize;

		

		int fpsCount;

		bool ReverseFlag;


		private void glControl_Load(object sender, EventArgs e) {

			System.Windows.Window MainWindow = System.Windows.Application.Current.MainWindow;
			MainWindowPresentationSource = PresentationSource.FromVisual(MainWindow);
			m = MainWindowPresentationSource.CompositionTarget.TransformToDevice;

			dpiX = m.M11;
			dpiY = m.M22;

		}


		private void Draw(){

			MapViewSize = Convert.ToInt32(glControl.Height * 0.75);

			GL.ClearColor(Color4.Black);
			
			// メイン画面

			GL.Enable(EnableCap.DepthTest);

			GL.Enable(EnableCap.Lighting);

			GL.DepthRange(0.1, 1.0);

			GL.MatrixMode(MatrixMode.Projection);       // 透視投影
			proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60.0f), glControl.AspectRatio, minDistance, MaxDistance);
			GL.LoadMatrix(ref proj);

			GL.MatrixMode(MatrixMode.Modelview);
			look = Matrix4.LookAt(position, position + front, up);
			GL.LoadMatrix(ref look);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.Viewport(0, 0, glControl.Width, glControl.Height);

			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

			GL.Material(MaterialFace.Front, MaterialParameter.Emission, Color4.Red);
			tube(2, 0.1f, 0.1f);

			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

			GL.Material(MaterialFace.Front, MaterialParameter.Emission, Color4.Green);
			square(1.5f);

			for (int n = 0; n < 100; n++) {
//				square(n);
			}


			// マップ表示

			GL.Viewport(glControl.Width - 20 - MapViewSize, glControl.Height - 20 - MapViewSize, MapViewSize, MapViewSize);
			
			GL.DepthRange(0.0, 0.1);

			GL.MatrixMode(MatrixMode.Projection);       // 平行投影
			proj = Matrix4.CreateOrthographic(5f, 5f, -10f, 10f);
			GL.LoadMatrix(ref proj);
			GL.MatrixMode(MatrixMode.Modelview);
			
			lookMap = Matrix4.LookAt(0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f);
			GL.LoadMatrix(ref lookMap);

			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

			GL.Material(MaterialFace.Front, MaterialParameter.Emission, Color4.Blue);
			tube(2, 0.1f, 0.1f);

			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

			GL.Material(MaterialFace.Front, MaterialParameter.Emission, Color4.Yellow);
			square(1.5f);

			for (int n = 0; n < 100; n++) {
//				square(n);
			}

			glControl.SwapBuffers();

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
			Get_Center();

		}

		private void Window_SizeChanged(object sender, EventArgs e) {

			Popup_Move();
			Get_Center();

		}

		private void Window_MouseMove(object sender, MouseEventArgs e) {        // WindowsFormHostのプロパティから共通→IsEnabledをOFFにすること
																				// チェックが入ったままだと描画領域上でマウスイベントが使えない
			if (CameraRotate) {

				if(!ReverseFlag){

					yaw += (System.Windows.Forms.Cursor.Position.X - lastPos[0]) * Sensitivity;

					if (yaw > 180f) {
						yaw -= 360f;
					} else if (yaw < -180f) {
						yaw += 360f;
					}

					pitch -= (System.Windows.Forms.Cursor.Position.Y - lastPos[1]) * Sensitivity;

					if (pitch > 89.0f) {
						pitch = 89.0f;
					} else if (pitch < -89.0f) {
						pitch = -89.0f;
					}

				}else{

					ReverseFlag = false;

				}

				lastPos[0] = System.Windows.Forms.Cursor.Position.X;
				lastPos[1] = System.Windows.Forms.Cursor.Position.Y;

				front[0] = (float)Math.Cos(MathHelper.DegreesToRadians(pitch)) * (float)Math.Cos(MathHelper.DegreesToRadians(yaw));
				front[1] = (float)Math.Sin(MathHelper.DegreesToRadians(pitch));
				front[2] = (float)Math.Cos(MathHelper.DegreesToRadians(pitch)) * (float)Math.Sin(MathHelper.DegreesToRadians(yaw));

				front = Vector3.Normalize(front);

				if (Math.Abs(lastPos[0] - CenterX) > 100 || Math.Abs(lastPos[1] - CenterY) > 100) {
					ReverseFlag = true;
					SetCursorPos(CenterX, CenterY);
				}

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

							SetCursorPos(CenterX, CenterY);

							lastPos[0] = System.Windows.Forms.Cursor.Position.X;
							lastPos[1] = System.Windows.Forms.Cursor.Position.Y;

							CameraLabel.Foreground = new SolidColorBrush(Colors.Magenta);

							this.Cursor = Cursors.None;

						}else{

							CameraLabel.Foreground = new SolidColorBrush(Colors.Transparent);

							this.Cursor = Cursors.Arrow;

						}

					}

					break;

				case Key.M:
					
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

		private void Get_Center() {

			if(this.WindowState == System.Windows.WindowState.Maximized) {																								// 最大化してもthis.LeftとTopは小さい状態での値を返すため一手間必要

				System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)this.Left, (int)this.Top, (int)this.ActualWidth, (int)this.ActualHeight);				// WPFはフォームと異なり現在のディスプレイサイズを得られないため，ダミーの四角形を作ってその座標を得ることで求める
				System.Windows.Forms.Screen screenData = System.Windows.Forms.Screen.FromRectangle(rect);
				
				CenterX = Convert.ToInt32((this.ActualWidth / 2 + glHost.Margin.Left / 2 - glHost.Margin.Right / 2) * dpiX + screenData.WorkingArea.Left);
				CenterY = Convert.ToInt32((this.ActualHeight / 2 + glHost.Margin.Top / 2 - glHost.Margin.Bottom / 2) * dpiY + screenData.WorkingArea.Top + 15.96);

			} else{
				CenterX = Convert.ToInt32((this.Left + this.ActualWidth / 2 + glHost.Margin.Left / 2 - glHost.Margin.Right / 2) * dpiX);
				CenterY = Convert.ToInt32((this.Top + this.ActualHeight / 2 + glHost.Margin.Top / 2 - glHost.Margin.Bottom / 2) * dpiY + 15.96);
			}			

		}


		private void SetupTimer() {

			var timer = new DispatcherTimer(DispatcherPriority.Normal) {
				Interval = TimeSpan.FromSeconds(0.001),
			};

			timer.Tick += (s, e) => {

				if (this.IsActive) {

					// シフトキーはスローにすべきか高速移動にすべきか

					if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
						ActSpeed = Speed / 10;
					} else {
						ActSpeed = Speed;
					}

					if (Keyboard.IsKeyDown(Key.A)) {
						position -= Vector3.Normalize(Vector3.Cross(front, up)) * ActSpeed;
					}
					if (Keyboard.IsKeyDown(Key.D)) {
						position += Vector3.Normalize(Vector3.Cross(front, up)) * ActSpeed;
					}
					if (Keyboard.IsKeyDown(Key.W)) {
						position += front * ActSpeed;
					}
					if (Keyboard.IsKeyDown(Key.S)) {
						position -= front * ActSpeed;
					}
					if (Keyboard.IsKeyDown(Key.Up)) {
						position += up * ActSpeed;
					}
					if (Keyboard.IsKeyDown(Key.Down)) {
						position -= up * ActSpeed;
					}

				}

				fpsCount++;

				Draw();

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