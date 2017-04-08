using HoloToolkit.Sharing;
using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EasySharingMessages : Singleton<EasySharingMessages> {

    /// <summary>
    /// メッセージを受信したときに、
    /// 何の操作についてのメッセージなのかを振り分けるためのIDのenum
    /// </summary>
    public enum TestMessageID : byte {
        HeadTransform = MessageID.UserMessageIDStart,   // HMDのTransform
        CharacterTransform,                             // 操作対象キャラクターTransform
        Max
    }

    public enum UserMessageChannels {
        Anchors = MessageChannel.UserMessageChannelStart
    }

    /// <summary>
    /// 自分自身のユーザID
    /// </summary>
    public long LocalUserId {
        get; set;
    }

    /// <summary>
    /// 受信したメッセージをハンドルするデリゲート
    /// </summary>
    /// <param name="msg"></param>
    public delegate void MessageCallBack(NetworkInMessage msg);

    private Dictionary<TestMessageID, MessageCallBack> messageHandlers = new Dictionary<TestMessageID, MessageCallBack>();
    /// <summary>
    /// 受信メッセージハンドラの辞書
    /// </summary>
    public Dictionary<TestMessageID, MessageCallBack> MessageHandlers {
        get { return messageHandlers; }
    }

    /// <summary>
    /// ここにメッセージハンドラを乗せていく
    /// なんと表現すればいいのか
    /// </summary>
    private NetworkConnectionAdapter connectionAdapter;
    private NetworkConnection serverConnection;

    /// <summary>
    /// サーバとのコネクションが張れたタイミングで初期化を行う
    /// </summary>
    private void Start() {
        if (SharingStage.Instance.IsConnected) {
            Connected();
        } else {
            SharingStage.Instance.SharingManagerConnected += Connected;
        }
    }

    /// <summary>
    /// サーバとの接続が完了したときのハンドラ
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Connected(object sender = null, EventArgs e = null) {
        SharingStage.Instance.SharingManagerConnected -= Connected;
        InitializeMessageHandler();
    }

    /// <summary>
    /// 初期化
    /// </summary>
    private void InitializeMessageHandler() {
        SharingStage sharingStage = SharingStage.Instance;
        // null check
        if (sharingStage == null) {
            Debug.Log("Cannot Initialize CustomMessages. No SharingStage instance found.");
            return;
        }

        serverConnection = sharingStage.Manager.GetServerConnection();
        // nullcheck
        if (serverConnection == null) {
            Debug.Log("Cannot Initialize CustomMessages. Cannot get a server connection.");
            return;
        }

        connectionAdapter = new NetworkConnectionAdapter();
        // SharingServiceからのメッセージを受信したときのハンドラを設定
        connectionAdapter.MessageReceivedCallback += OnMessageReceived;
        // 自分のユーザIDを取得
        LocalUserId = SharingStage.Instance.Manager.GetLocalUser().GetID();
        // 受信メッセージハンドラのdictionaryの箱を用意しておく
        for (byte index = (byte)TestMessageID.HeadTransform; index < (byte)TestMessageID.Max; index++) {
            if (MessageHandlers.ContainsKey((TestMessageID)index) == false) {
                MessageHandlers.Add((TestMessageID)index, null);
            }
            serverConnection.AddListener(index, connectionAdapter);
        }
    }

    /// <summary>
    /// SharingServiceからメッセージを受信したときのハンドラ
    /// メッセージタイプ(TestMessageIDのいずれか)に対応させるハンドラを決定する
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="msg"></param>
    private void OnMessageReceived(NetworkConnection connection, NetworkInMessage msg) {
        // メッセージタイプの取り出し
        byte messageType = msg.ReadByte();
        // 対応させるハンドラの決定してコール
        MessageCallBack messageHandler = MessageHandlers[(TestMessageID)messageType];
        if (messageHandler != null) {
            messageHandler(msg);
        }
    }

    /// <summary>
    /// 送信メッセージの作成
    /// メッセージタイプとユーザIDだけメッセージに書き込む
    /// </summary>
    /// <param name="messageType"></param>
    /// <returns></returns>
    private NetworkOutMessage CreateMessage(byte messageType) {
        NetworkOutMessage msg = serverConnection.CreateMessage(messageType);
        msg.Write(messageType);     // メッセージタイプ
        msg.Write(LocalUserId);     // ユーザID
        return msg;
    }

    /// <summary>
    /// カメラのtransformをBroadcastしてもらうようサーバに依頼する
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public void SendHeadTransform(Vector3 position, Quaternion rotation) {
        if (serverConnection != null && serverConnection.IsConnected()) {
            NetworkOutMessage msg = CreateMessage((byte)TestMessageID.HeadTransform);
            AppendHeadTransform(msg, position, rotation);
            serverConnection.Broadcast(
                msg, MessagePriority.Immediate, MessageReliability.UnreliableSequenced, MessageChannel.Avatar);
        }
    }

    /// <summary>
    /// 自機のtransformをBroadcastしてもらうようサーバに依頼する
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="scale"></param>
    public void SendSelfCharacterTransform(Vector3 position, Quaternion rotation, Vector3 scale, int characterTypeInt) {
        if(serverConnection !=null && serverConnection.IsConnected()) {
            NetworkOutMessage msg = CreateMessage((byte)TestMessageID.CharacterTransform);
            // Transformをメッセージに貼り付け
            AppendCharacterTransform(msg, position, rotation, scale);
            // CharacterTypeをメッセージに貼り付け
            msg.Write(characterTypeInt);
            serverConnection.Broadcast(
                msg, MessagePriority.Immediate, MessageReliability.UnreliableSequenced, MessageChannel.Avatar);
        }
    }

    /// <summary>
    /// 最終処理、ハンドラの跡片付け
    /// </summary>
    protected override void OnDestroy() {
        base.OnDestroy();

        if (serverConnection != null) {
            for (byte index = (byte)TestMessageID.HeadTransform; index < (byte)TestMessageID.Max; index++) {
                serverConnection.RemoveListener(index, connectionAdapter);
            }
            connectionAdapter.MessageReceivedCallback -= OnMessageReceived;
        }
    }


    #region HeadTransferMessageHelper
    private void AppendHeadTransform(NetworkOutMessage msg, Vector3 position, Quaternion rotation) {
        AppendVector3(msg, position);
        AppendQuaternion(msg, rotation);
    }

    private void AppendCharacterTransform(NetworkOutMessage msg, Vector3 position, Quaternion rotation, Vector3 scale) {
        AppendVector3(msg, position);
        AppendQuaternion(msg, rotation);
        AppendVector3(msg, scale);
    }

    private void AppendVector3(NetworkOutMessage msg, Vector3 position) {
        msg.Write(position.x);
        msg.Write(position.y);
        msg.Write(position.z);
    }

    private void AppendQuaternion(NetworkOutMessage msg, Quaternion rotation) {
        msg.Write(rotation.x);
        msg.Write(rotation.y);
        msg.Write(rotation.z);
        msg.Write(rotation.w);
    }

    public Vector3 ReadVector3(NetworkInMessage msg) {
        return new Vector3(msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat());
    }

    public Quaternion ReadQuaternion(NetworkInMessage msg) {
        return new Quaternion(msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat());
    }
    #endregion
}