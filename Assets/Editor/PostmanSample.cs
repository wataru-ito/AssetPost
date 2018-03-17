using System.IO;



/// <summary>
/// シンプルな例：テキストは全部 text
/// </summary>
public class SimplePostman : AssetPost.AssetPostman
{
	public override string Delivery(string fileName)
	{
		return Path.GetExtension(fileName) == ".txt" ?
			"Assets/Texts/" + fileName :
			null;
	}
}


/// <summary>
/// 大量に存在するアイテム画像。
/// 命名規約に従って適切なフォルダに配置する。
/// 命名規約：
///		item_(カテゴリ3文字)_(サブカテゴリ2文字)(ID4桁)
///	フォルダ：
///		Assets/Items/カテゴリ/サブカテゴリ/
/// </summary>
public class TexturePostman : AssetPost.AssetRegexPostman
{
	readonly char[] kSeparator = new char[] { '_', '.' };

	public TexturePostman() :
		base(@"^item_[a-zA-Z]{3}_[a-zA-Z]{2}\d{4}\.(png|PNG|jpg|JPG)$")
	{}

	protected override string FileName2AssetPath(string fileName)
	{
		// item_xxx_xx0000.png
		var elements = fileName.Split(kSeparator);
		return string.Format("Assets/Items/{0}/{1}/{2}",
			elements[1],
			elements[2].Remove(2),
			fileName);
	}
}