using Android.Content;
using Android.Net.Wifi;
using Android.Net;
using Android.App;
using Android.Widget;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Text;

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
//		MainActivity		mParent = null;

		Application			_Application = null;

		ConnectivityManager	mConnectivityManager = null;

		WifiManager			mWifiManager = null;

		TextView			_StatusView = null;


		/// <summary>
		/// コンストラクタ
		/// </summary>
		public NetworkStateReceiver(MainActivity parent)
		{
			_Application = parent.Application;

			mConnectivityManager = (ConnectivityManager)parent.GetSystemService(Context.ConnectivityService);

			mWifiManager = (WifiManager)parent.GetSystemService(Context.WifiService);

			_StatusView = parent.FindViewById<TextView>(Resource.Id.StatusView);
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
//			_StatusView.Text = "";

			NetworkInfo activeNetworkInfo = mConnectivityManager.ActiveNetworkInfo;

			bool isOnline = (activeNetworkInfo != null) && activeNetworkInfo.IsConnected;


			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("SDK Build Version : {0}\n", Build.VERSION.Sdk);
			builder.AppendFormat("NetworkState : {0}\n", (isOnline ? "Online" : "Offline"));

			if(isOnline) {
				builder.AppendFormat("ConnectType : {0}\n", activeNetworkInfo.TypeName);

				switch(activeNetworkInfo.Type) {
				case ConnectivityType.Wifi:
					WifiInfo info = mWifiManager.ConnectionInfo;
					builder.AppendFormat("BSSID : {0}\n", info.BSSID);
					builder.AppendFormat("SSID : {0}\n", info.SSID);

					byte[] byteArray = BitConverter.GetBytes(info.IpAddress);
					Java.Net.InetAddress inetAddress = Java.Net.InetAddress.GetByAddress(byteArray);
					string ipaddress = inetAddress.HostAddress;
					builder.AppendFormat("IpAddress : {0}\n", ipaddress);
					break;
				case ConnectivityType.Mobile:
					break;
				default: break;
				}
			}

			_StatusView.Text = builder.ToString();
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
				Toast.MakeText(_Application, "Wifi Connecting. SSID = " + candidacy.Ssid, ToastLength.Long).Show();
			}
		}
	}

}
