# AssetPost
ウィンドウにアセットをドロップすると、適切なフォルダに配置してくれるツールです。
アセットパスを求めるのにファイル名を使用します。

アセットの命名規約を作り、取り込みをスムーズにしましょう！


# こんな時に便利
大量に存在するファイルを、命名規約に従って指定のフォルダにいけないとき。
ファイル名やフォルダ名に誤字があってうまく認識されてなかった…などなど。
こうしたヒューマンエラーを予防します。


# 起動方法

Tools > AssetPost

このウィンドウがアセットポスト。
ここにポイポイするとプロジェクトの適切な場所に入れてくれる。
![AssetPostイメージ](./Readme_files/assetpost_image.jpg "image")


# そのためにはPostmanを定義する

AssetPost.AssetPostmanを継承したクラスを作っておくだけでOK。

### サンプル
例えば大量に存在するアイテム画像。
命名規約に従って適切なフォルダに配置する。
`ファイル命名規約 : item_XXX_YY0000.png`
`パス : Assets/Item/XXX/YY/ `

```C#
public class ItemTexturePostman : AssetPost.AssetRegexPostman
{
	// 対象ファイルの正規表現
	const string kPattern = @"^item_[a-zA-Z]{3}_[a-zA-Z]{2}\d{4}\.(png|PNG|jpg|JPG)$";
	
	public TexturePostman() : base(kPattern)
	{}

	protected override string FileName2AssetPath(string fileName)
	{
		// item_xxx_xx0000.png
		var elements = fileName.Split(new char[] { '_', '.' });
		return string.Format("Assets/Items/{0}/{1}/{2}",
			elements[1],
			elements[2].Remove(2),
			fileName);
	}
}
```

