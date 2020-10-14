using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Compiler42 {
	class LoadDDS {

		public int ReadDDS(String filepath){

			filepath = filepath.Replace("/", "\\");

			if(!File.Exists(filepath)){
				filepath = "Texture/404Texture_1024";		// テクスチャファイルがない場合は市松模様を描画
				return 0;
			} else{
				return 1;
			}

		}

	}
}
