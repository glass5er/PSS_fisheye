/* SCE CONFIDENTIAL
 * PlayStation(R)Suite SDK 0.98.2
 * Copyright (C) 2012 Sony Computer Entertainment Inc.
 * All Rights Reserved.
 * 
 * Fisheye Distortion Sample Program
 * Author : Kentaro Doba <glass5er@gmail.com>
 * Date   : 2012/04/22
 */

using System;

using Sce.Pss.Core;
using Sce.Pss.Core.Environment;

using Sce.Pss.Core.Graphics;
using Sce.Pss.Core.Imaging;
using Sce.Pss.Core.Input;


namespace Sample
{
	public class AppMain
	{
		//  default graphic context  //
		static protected GraphicsContext graphics;
		//  shader  //
		static ShaderProgram shaderProgram;
		//  texture  //
		static Texture2D texture;
		
		//  polygon vertex coordinates (to be determined)  //
		const int sliceNumV = 24;//5*2 + 1;
		const int sliceNumH = 24;//5*2 + 1;
		const int sliceNumTotal = sliceNumH * sliceNumV;
		
		//  
		const int sliceNumDraw = 2 * sliceNumH;
		
		//  vertex coordinates (TBD)  //
		static float[] vertices=new float[sliceNumTotal * 3];
		//  texture coordinates (TBD)  //
		static float[] texcoords = new float[sliceNumTotal * 2];
		//  texture screen colors (TBD)  //
		static float[] colors = new float[sliceNumTotal * 4];
		
		//  distortion coefs  //
		static float[] ud_coefs = {
			3.5f, 3.5f, 0.0f, 0.0f	
		};

		const int indexSize = sliceNumDraw;
		static ushort[][] indices;
		
		static int sign = -1;
		static float objRatio = 5.0f;
		static float objRelativeWidth, objRelativeHeight;
		static int screenWidth, screenHeight;

		static VertexBuffer vertexBuffer;
		
		// Width of texture.
		static float Width;

		// Height of texture.
		static float Height;
		
		static Matrix4 unitScreenMatrix;
		
		//--------//
		//  main  //
		//--------//
		public static void Main (string[] args)
		{
			Initialize ();
			
			//  PSS MainLoop  //
			while (true) {
				SystemEvents.CheckEvents ();
				Update ();
				Render ();
			}
		}

		public static void Initialize ()
		{
			//  define screen  //
			graphics = new GraphicsContext();
			ImageRect rectScreen = graphics.Screen.Rectangle;
			screenWidth = rectScreen.Width;
			screenHeight = rectScreen.Height;
			
			//  read image for texture  //
			//texture = new Texture2D("/Application/resources/Player.png", false);
			texture = new Texture2D("/Application/resources/cat.png", false);
			//  compile shader  //
			shaderProgram = new ShaderProgram("/Application/shaders/Sprite.cgx");
			shaderProgram.SetUniformBinding(0, "u_WorldMatrix");
			
			//  get texture image size  //
			Width = texture.Width;
			Height = texture.Height;
			
			//  calc vertex coordinates  //
			for(int i = 0; i<sliceNumTotal * 4; i++) {
				colors[i] = 1.0f;
			}
			calcVertices();
			
			//  texture mapping order  //
			indices = new ushort[sliceNumV-1][];
			for(int iy=0; iy<sliceNumV-1; iy++) {
				int cur_idx = 0;
				int ref_idx = iy * sliceNumH;
				indices[iy] = new ushort[sliceNumDraw];
				for(int ix=0; ix<sliceNumH; ix++) {
					indices[iy][cur_idx++] = (ushort)ref_idx;
					ref_idx += sliceNumH;
					indices[iy][cur_idx++] = (ushort)ref_idx;
					ref_idx += 1 - sliceNumH;
				}
			}
			
			//  construct vertex shader  //
			//  vertex_num(total), vertex_num(used with index), vertex_format, texture_format, color_format  //
			vertexBuffer = new VertexBuffer(sliceNumTotal, indexSize, VertexFormat.Float3, VertexFormat.Float2, VertexFormat.Float4);
			
			//  set {vertex, texture, color}  //
			vertexBuffer.SetVertices(0, vertices);
			vertexBuffer.SetVertices(1, texcoords);
			vertexBuffer.SetVertices(2, colors);
			
			//  set shader  //
			graphics.SetVertexBuffer(0, vertexBuffer);
			
			objRelativeWidth  = Width*objRatio/screenWidth;
			objRelativeHeight = Height*objRatio/screenHeight;
			unitScreenMatrix = new Matrix4(
				 objRelativeWidth,	0.0f,  0.0f, 0.0f,
				 0.0f,   -objRelativeHeight,	0.0f, 0.0f,
				 0.0f,   0.0f, 1.0f, 0.0f,
				 -objRelativeWidth/2.0f,  objRelativeHeight/2.0f, 0.0f, 1.0f
			);
		
		}

		public static void Update ()
		{
			//  update size ratio  //
			if(objRatio > 5.0f) {
				sign = -1;
			}
			if(objRatio < 4.0f) {
				sign = 1;
			}
			objRatio += (sign * 0.015625f);
			objRelativeWidth  = Width*objRatio/screenWidth;
			objRelativeHeight = Height*objRatio/screenHeight;
			//  write ratio to matrix  //
			unitScreenMatrix.M11 = objRelativeWidth;
			unitScreenMatrix.M22 = -objRelativeHeight;
			unitScreenMatrix.M41 = -objRelativeWidth / 2.0f;
			unitScreenMatrix.M42 = objRelativeHeight / 2.0f;
		}

		public static void Render ()
		{
			//  init screen  //
			graphics.Clear();
			
			//  link shader  //
			graphics.SetShaderProgram(shaderProgram);
			graphics.SetTexture(0, texture);
			shaderProgram.SetUniformValue(0, ref unitScreenMatrix);
			
			//  draw every row  //
			for(int iy = 0; iy<sliceNumV/2; iy++) {
				vertexBuffer.SetIndices(indices[iy]);
				graphics.DrawArrays(DrawMode.TriangleStrip, 0, indexSize);
			}
			for(int iy = sliceNumV-2; iy>=sliceNumV/2; iy--) {
				vertexBuffer.SetIndices(indices[iy]);
				graphics.DrawArrays(DrawMode.TriangleStrip, 0, indexSize);
			}
			//  finish drawing  //
			graphics.SwapBuffers();	
		}
		
		public static void calcVertices () {
			//  calc vertex coordinates  //
			for(int iy = 0; iy<sliceNumV; iy++) {
				for(int ix = 0; ix<sliceNumH; ix++) {
					//  texture index  //
					int idx_t = (iy * sliceNumH + ix) * 2;
					//  vertex index  //
					int idx_v = (iy * sliceNumH + ix) * 3;
					//  texture coordinates  //
					float tmp_tx = (float)ix/(float)(sliceNumH - 1);
					float tmp_ty = (float)iy/(float)(sliceNumV - 1);
					texcoords[idx_t + 0] = tmp_tx;    //  x
					texcoords[idx_t + 1] = tmp_ty;    //  y
					//  vertex coordinates  //
					float tmp_vx = (float)ix/(float)(sliceNumH - 1);
					float tmp_vy = (float)iy/(float)(sliceNumV - 1);
					undistort_wrap(ref tmp_vx, ref tmp_vy);
					vertices[idx_v    ] = tmp_vx;	// x
					vertices[idx_v + 1] = tmp_vy;	// y
					vertices[idx_v + 2] = 0.0f;	// z
				}
			}
			

		}
		
		public static void distort_wrap(ref float rx, ref float ry) {
			//  translate  //
			rx -= 0.5f;
			ry -= 0.5f;
			//  distort_core  //
			distort(ref rx, ref ry);
			//  translate  //
			rx += 0.5f;
			ry += 0.5f;
		}
		public static void undistort_wrap(ref float rx, ref float ry) {
			//  translate  //
			rx -= 0.5f;
			ry -= 0.5f;
			//  undistort_core  //
			undistort(ref rx, ref ry);
			//  translate  //
			rx += 0.5f;
			ry += 0.5f;
		}
		public static void distort(ref float rx, ref float ry) {
			float xy = rx * ry;
			float x2 = rx * rx;
			float y2 = ry * ry;
			float r2 = x2 + y2;
			//  radial distortion  //
			float k_rad = 1.0f + ud_coefs[0] * r2 + ud_coefs[1] * r2*r2;
			//  circumferential direction  //
			float dx = 2.0f * ud_coefs[2] * xy + ud_coefs[3] * (r2+2*x2);
			float dy = ud_coefs[2] * (r2 + 2*y2) + 2 * ud_coefs[3] * xy;
			//  distort  //
			rx = (rx-dx) / k_rad;
			ry = (ry-dy) / k_rad;
		}
		public static void undistort(ref float rx, ref float ry) {
			float xy = rx * ry;
			float x2 = rx * rx;
			float y2 = ry * ry;
			float r2 = x2 + y2;
			//  radial distortion  //
			float k_rad = 1.0f + ud_coefs[0] * r2 + ud_coefs[1] * r2*r2;
			//  circumferential direction  //
			float dx = 2.0f * ud_coefs[2] * xy + ud_coefs[3] * (r2+2*x2);
			float dy = ud_coefs[2] * (r2 + 2*y2) + 2 * ud_coefs[3] * xy;
			//  undistort  //
			rx = rx * k_rad + dx;
			ry = ry * k_rad + dy;
		}
	}
}
