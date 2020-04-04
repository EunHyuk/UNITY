using System;
using System.Net;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using XLua;

public delegate void ReqCallback(string info);
public delegate void DownloadCallback();

public class OtkRequestInfo {
	public LuaFunction succeedCall;
	public LuaFunction failedCall;
	public ReqCallback succeedCallback;
	public ReqCallback failedCallback;

	public string result;
	public bool succeed;
	public bool isLua;
};

public class DownloadProgress {
    public long totalBytes;
    public long bytesReceived;
    public int progress;
}

public class DownloadFileInfo {
	public string fileURL;
	public string realFilePath;

    public string tmpFilePath;

	public DownloadCallback succeedCallback;
	public DownloadCallback failedCallback;

    public LuaFunction progressCallback;

	public bool succeed;
};

public class OtkClient
{
	public static void SendOtkHttpRequest(string data, string url, ReqCallback succeedCall, ReqCallback failedCall)
	{
		Debug.Log ("client send http request:" + url);
		Debug.Log ("request data:" + data);
		var info = new OtkRequestInfo ();
		info.succeedCallback = succeedCall;
		info.failedCallback = failedCall;

		HttpWebRequest req = (HttpWebRequest)WebRequest.Create (url);
		req.Method = "POST";
		req.Timeout = 30000;
		req.KeepAlive = false;

		HttpWebResponse resp = null;
		byte[] responseContent = null;

		var worker = new BackgroundWorker();

		worker.DoWork += (sender, args) => {
			var sendData = EncryptMgr.EncodeData(data);
			req.ContentLength = sendData.Length;
			Stream writeStream = req.GetRequestStream();
			writeStream.Write(sendData, 0, sendData.Length);
			writeStream.Close();

			resp = (HttpWebResponse)req.GetResponse();

			MemoryStream ms = new MemoryStream();
			using (Stream responseStream = resp.GetResponseStream())
			{
				byte[] buffer = new byte[0x1000];
				int bytes;
				while ((bytes = responseStream.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, bytes);
				}
			}

			responseContent = ms.ToArray();

			ms.Close();
			resp.Close();
		};

		worker.RunWorkerCompleted += (sender, e) => {
			if (e.Error != null) {
				Debug.Log("error:" + e.Error);
//				info.result = "{\"Status\":-1}";
//				{"Resp":{"RespCode":0,"RespDesc":""},"Content":""}
                info.result = "{\"Resp\":{\"RespCode\":-1, \"RespCodeStr\":\"-1\", \"RespDesc\":\"网络错误\", \"RespDescCN\":\"网络错误\"}}";
				info.succeed = false;
				ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_CALLBACK, info);
			} else {
				if (resp.StatusCode == HttpStatusCode.OK) {
					info.result = EncryptMgr.DecodeData(responseContent);
					info.succeed = true;
					info.isLua = false;
					Debug.Log("resp:"+info.result);
					ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_CALLBACK, info);
				} else {
					Debug.Log("Status code:"+resp.StatusCode+","+resp.StatusDescription);
//					info.result = "{\"Status\":-2, \"Content\":"+resp.StatusDescription+"}";
                    info.result = "{\"Resp\":{\"RespCode\":-2, \"RespCodeStr\":\"-2\", \"RespDesc\":"+resp.StatusDescription+", \"RespDescCN\":" + resp.StatusDescription + "}}";
					info.succeed = false;
					info.isLua = false;
					ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_CALLBACK, info);
				}
			}
		};
		worker.RunWorkerAsync();
	}

	public static void SendOtkHttpRequest(string data, string url, LuaFunction succeedCall, LuaFunction failedCall)
	{
		Debug.Log ("send http request:"+url);

		var info = new OtkRequestInfo ();
		info.succeedCall = succeedCall;
		info.failedCall = failedCall;

		HttpWebRequest req = (HttpWebRequest)WebRequest.Create (url);
		req.Method = "POST";
		req.Timeout = 30000;
		req.KeepAlive = false;

		HttpWebResponse resp = null;
		byte[] responseContent = null;

		var worker = new BackgroundWorker();

		worker.DoWork += (sender, args) => {
			var sendData = EncryptMgr.EncodeData(data);
			req.ContentLength = sendData.Length;
			Stream writeStream = req.GetRequestStream();
			writeStream.Write(sendData, 0, sendData.Length);
			writeStream.Close();

			resp = (HttpWebResponse)req.GetResponse();

			MemoryStream ms = new MemoryStream();
			using (Stream responseStream = resp.GetResponseStream())
			{
				byte[] buffer = new byte[0x1000];
				int bytes;
				while ((bytes = responseStream.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, bytes);
				}
			}

			responseContent = ms.ToArray();

			ms.Close();
			resp.Close();
		};

		worker.RunWorkerCompleted += (sender, e) => {
			if (e.Error != null) {
				Debug.Log("error:"+e.Error);
//				info.result = "{\"Status\":-1}";
                info.result = "{\"Resp\":{\"RespCode\":-1, \"RespCodeStr\":\"-1\", \"RespDesc\":\"网络错误\", \"RespDescCN\":\"网络错误\"}}";
				info.succeed = false;
				ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_CALLBACK, info);
			} else {
				if (resp.StatusCode == HttpStatusCode.OK) {
					info.result = EncryptMgr.DecodeData(responseContent);
					info.succeed = true;
					info.isLua = true;
					ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_CALLBACK, info);
				} else {
					Debug.Log("Status code:"+resp.StatusCode+","+resp.StatusDescription);
//					info.result = "{\"Status\":-2, \"Content\":"+resp.StatusDescription+"}";
                    info.result = "{\"Resp\":{\"RespCode\":-2, \"RespCodeStr\":\"-2\", \"RespDesc\":"+resp.StatusDescription+", \"RespDescCN\":" + resp.StatusDescription + "}}";
					info.succeed = false;
					info.isLua = true;
					ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_CALLBACK, info);
				}
			}
		};
		worker.RunWorkerAsync();
	}

    public static void DownloadFile(DownloadFileInfo info)
    {
        WebClient client = new WebClient();

        client.DownloadProgressChanged += new DownloadProgressChangedEventHandler((object sender, DownloadProgressChangedEventArgs e)=>{
            OtkCallback call = (System.Object obj) =>
            {
                if (info.progressCallback != null) {
                    DownloadProgress downloadProgress = new DownloadProgress();
                    downloadProgress.bytesReceived = e.BytesReceived;
                    downloadProgress.totalBytes = e.TotalBytesToReceive;
                    downloadProgress.progress = e.ProgressPercentage;

                    info.progressCallback.Action(downloadProgress);
                }
            };

            ConsumerMgr.getInstance().pushMsg(MsgType.MSG_ON_DO_IN_MAIN_CALLBACK, call);

        });
        client.DownloadFileCompleted += new AsyncCompletedEventHandler((object sender, AsyncCompletedEventArgs e)=>{
            if (e.Error != null) {
                Debug.Log("download file error:"+e.Error);
                info.succeed = false;
                ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_DOWNLOAD_CALLBACK, info);
            } else if (File.Exists(info.tmpFilePath)) {
                var values = info.realFilePath.Split('_');
                var serverMd5 = values[values.Length - 1];
                var md5 = EncryptMgr.hashFile(info.tmpFilePath);
                if (serverMd5 != md5) {
                    Debug.Log("download failed, md5 not the same, " + info.fileURL);
                    info.succeed = false;
                    ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_DOWNLOAD_CALLBACK, info);
                } else {
                    Debug.Log("download succeed:" + info.fileURL);
                    File.Copy(info.tmpFilePath, info.realFilePath);
                    File.Delete(info.tmpFilePath);
                    info.succeed = true;
                    ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_DOWNLOAD_CALLBACK, info);
                }
            } else {
                Debug.Log("download failed, file not exists:" + info.realFilePath);
                info.succeed = false;
                ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_DOWNLOAD_CALLBACK, info);
            }

            client.Dispose();
        });

        Uri uri;
        bool succeed = Uri.TryCreate(info.fileURL, UriKind.Absolute, out uri);
        if (succeed) {
            client.DownloadFileAsync(uri, info.tmpFilePath);
        } else {
            Debug.Log("download failed, invalid url:" + info.fileURL);
            info.succeed = false;
            ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_DOWNLOAD_CALLBACK, info);

            client.Dispose();
        }
    }

	//public static void DownloadFile(DownloadFileInfo info) {
	//	Debug.Log ("client send download request:"+info.fileURL);

	//	HttpWebRequest req = (HttpWebRequest)WebRequest.Create (info.fileURL);
	//	req.Method = "GET";
	//	req.Timeout = 30000;
	//	req.KeepAlive = false;

	//	HttpWebResponse resp = null;
	//	byte[] responseContent = null;

	//	var worker = new BackgroundWorker();

	//	worker.DoWork += (sender, args) => {
	//		resp = (HttpWebResponse)req.GetResponse();

	//		MemoryStream ms = new MemoryStream();
	//		using (Stream responseStream = resp.GetResponseStream())
	//		{
	//			byte[] buffer = new byte[0x1000];
	//			int bytes;
	//			while ((bytes = responseStream.Read(buffer, 0, buffer.Length)) > 0)
	//			{
	//				ms.Write(buffer, 0, bytes);
	//			}
	//		}

	//		responseContent = ms.ToArray();

	//		ms.Close();
	//		resp.Close();
	//	};

	//	worker.RunWorkerCompleted += (sender, e) => {
	//		if (e.Error != null) {
	//			Debug.Log("error:"+e.Error);
	//			info.succeed = false;
	//			ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_DOWNLOAD_CALLBACK, info);
	//		} else {
	//			if (resp.StatusCode == HttpStatusCode.OK) {
	//				info.succeed = true;
	//				FileInfo f = new FileInfo(info.realFilePath);
	//				FileStream fs = f.Create();
	//				fs.Write(responseContent, 0, responseContent.Length);
	//				fs.Close();

	//				ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_DOWNLOAD_CALLBACK, info);
	//			} else {
	//				Debug.Log("download status code:"+resp.StatusCode+","+resp.StatusDescription);
	//				info.succeed = false;
	//				ConsumerMgr.getInstance().pushMsg(MsgType.MSG_OTKCLIENT_DOWNLOAD_CALLBACK, info);
	//			}
	//		}
	//	};
	//	worker.RunWorkerAsync();
	//}
}