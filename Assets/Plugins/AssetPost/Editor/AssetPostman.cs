using System.Text.RegularExpressions;

namespace AssetPost
{
	/// <summary>
	/// AssetPostWindow起動時にクラスのインスタンスが作られる。
	/// 継承したクラスはデフォルトコンストラクタを実装する事
	/// </summary>
	class AssetPostman
	{
		AssetPostAddress m_adress;
		readonly Regex m_fileNameRegex;

		public AssetPostman(AssetPostAddress adress)
		{
			m_adress = adress;
			m_fileNameRegex = new Regex(adress.fileNamePattern);
		}

		/// <summary>
		/// ファイル名からアセットパスに変換する
		/// null or empty で無視
		/// </summary>
		public string Delivery(string fileName)
		{
			return m_fileNameRegex.IsMatch(fileName) ?
				m_adress.GetAssetPath(fileName) : null;
		}
	}
}