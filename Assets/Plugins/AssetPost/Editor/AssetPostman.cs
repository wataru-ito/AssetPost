using System.Text.RegularExpressions;

namespace AssetPost
{
	/// <summary>
	/// AssetPostWindow起動時にクラスのインスタンスが作られる。
	/// 継承したクラスはデフォルトコンストラクタを実装する事
	/// </summary>
	public abstract class AssetPostman
	{
		readonly Regex fileNameRegex;

		/// <summary>
		/// ファイル名からアセットパスに変換する
		/// null or empty で無視
		/// </summary>
		public abstract string Delivery(string fileName);
	}

	/// <summary>
	/// 正規表現にマッチしたファイルを配置するAssetPostman
	/// </summary>
	public abstract class AssetRegexPostman : AssetPostman
	{
		readonly Regex fileNameRegex;

		public AssetRegexPostman(string pattern)
		{
			fileNameRegex = new Regex(pattern);
		}

		public sealed override string Delivery(string fileName)
		{
			return fileNameRegex.IsMatch(fileName) ?
				FileName2AssetPath(fileName) : null;
		}

		/// <summary>
		/// ファイル名からアセットパスに変換する
		/// 渡されるファイル名はコンストラクタで渡した正規表現にマッチした奴のみ
		/// </summary>
		protected abstract string FileName2AssetPath(string fileName);
	}
}