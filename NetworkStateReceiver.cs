using Android.Content;
using Android.Net.Wifi;
using Android.Net;
using Android.App;
using Android.Widget;
using System;
using System.Collections.Generic;

namespace NetworkDeviceSwitch
{
	/// <summary>
	/// 通信状態の切り替わり時にブロードキャストされ、その時のネットワークデバイス情報を取得、表示するクラス
	/// 以下の登録方法はアプリがバックグラウンドにいる間もブロードキャストされるので
	/// 今回はそこまで必要ないのでActivity内でインスタンス化して登録する
	/// </summary>
//	[BroadcastReceiver]
//	[IntentFilter(new[] { MainActivity.CONNECTIVITY_CHANGE })]
	public class NetworkStateReceiver : BroadcastReceiver
	{
		MainActivity		mParent = null;

		ConnectivityManager	mConnectivityManager = null;

		WifiManager			mWifiManager = null;


		/// <summary>
		/// コンストラクタ
		/// </summary>
		public NetworkStateReceiver(MainActivity parent)
		{
			mParent = parent;

			mConnectivityManager = (ConnectivityManager)mParent.GetSystemService(Context.ConnectivityService);

			mWifiManager = (WifiManager)mParent.GetSystemService(Context.WifiService);
		}

		/// <summary>
		/// ブロードキャスト受取
		/// UIスレッドで実行される
		/// </summary>
		/// <param name="context"></param>
		/// <param name="intent"></param>
		public override void OnReceive(Context context, Intent intent)
		{
			System.Diagnostics.Debug.WriteLine("接続に変化がありました " + intent.Action);
			// 別のアプリからフォーカス戻したときも以下のアクションでレシーブされる。なんでだ
			if(intent.Action == MainActivity.SCAN_RESULTS) {
				// 最寄りの登録済みAPに接続を試みる
				TryConnectAP();
			}
			CheckNetworkState();
		}

		/// <summary>
		/// 通信情報確認
		/// </summary>
		void CheckNetworkState()
		{
			mParent.StatusView.Text = "";

			NetworkInfo activeNetworkInfo = mConnectivityManager.ActiveNetworkInfo;

			bool isOnline = (activeNetworkInfo != null) && activeNetworkInfo.IsConnected;

			mParent.StatusView.Text += string.Format("NetworkState : {0}\n", (isOnline ? "Online" : "Offline"));

			if(isOnline) {
				mParent.StatusView.Text += string.Format("ConnectType : {0}\n", activeNetworkInfo.TypeName);

				switch(activeNetworkInfo.Type) {
				case ConnectivityType.Wifi:
					WifiInfo info = mWifiManager.ConnectionInfo;
					mParent.StatusView.Text += string.Format("BSSID : {0}\n", info.BSSID);
					mParent.StatusView.Text += string.Format("SSID : {0}\n", info.SSID);

					byte[] byteArray = BitConverter.GetBytes(info.IpAddress);
					Java.Net.InetAddress inetAddress = Java.Net.InetAddress.GetByAddress(byteArray);
					string ipaddress = inetAddress.HostAddress;
					mParent.StatusView.Text += string.Format("IpAddress : {0}\n", ipaddress);
					break;
				case ConnectivityType.Mobile:
					break;
				default: break;
				}
			}
		}


		/// <summary>
		/// Try connect to Access Point.
		/// </summary>
		void TryConnectAP()
		{
			// APスキャン結果
			IList<ScanResult> results = mWifiManager.ScanResults;

			Android.Util.Log.Info("TryConnectAP", "Scan Resut Start");
			foreach(var result in results) {
				Android.Util.Log.Info("TryConnectAP", "		ssid {0}", result.Ssid);
			}
			Android.Util.Log.Info("TryConnectAP", "Scan Resut End.");

			// 端末に保存されているネットワーク設定リスト
			var ConfiguredNetworks = mWifiManager.ConfiguredNetworks;

			// スキャンしたAPのSSIDと設定リストに登録されてるSSIDで一致するものがあり
			// なおかつその中で一番Frequencyが高いやつに接続する
			WifiConfiguration candidacy = null;
			int frequency = 0;
			foreach(var Config in ConfiguredNetworks) {
				foreach(var result in results) {
					var ssid = Config.Ssid.Replace("\"", ""); // 接頭、接尾の["]が邪魔なので削除する
					if(ssid.Equals(result.Ssid)) {
						if(frequency < result.Frequency) {
							candidacy = Config;	// 接続候補
							frequency = result.Frequency;
						}
					}
				}
			}

			if(candidacy != null) {
				// すでにどこかと接続中だった場合の切断処理入れてないけど問題ないか？
				mWifiManager.EnableNetwork(candidacy.NetworkId, true);
				Toast.MakeText(mParent, "Wifi Connecting. SSID = " + candidacy.Ssid, ToastLength.Long).Show();
			}
		}
	}

}
