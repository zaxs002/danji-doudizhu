//-----------------------------------------------------------------------
// <copyright file="NFCKernelModule.cs">
//     Copyright (C) 2015-2015 lvsheng.huang <https://github.com/ketoo/NFrame>
// </copyright>
//-----------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NFMsg;
using ProtoBuf;

namespace NFSDK {
    public class NFCKernelModule : NFIKernelModule {
        private static NFCKernelModule _instance = null;

        public static NFCKernelModule Instance() {
            return _instance;
        }

        public NFCKernelModule(NFIPluginManager pluginManager) {
            _instance = this;
            mPluginManager = pluginManager;
            mhtObject = new Dictionary<NFGUID, NFIObject>();
            mhtClassHandleDel = new Dictionary<string, ClassHandleDel>();
        }

        ~NFCKernelModule() {
            mhtObject = null;
        }

        public override bool Awake() {
            return true;
        }

        public override bool Init() {
            mxElementModule = FindModule<NFIElementModule>();
            mxLogicClassModule = FindModule<NFILogicClassModule>();
            return true;
        }

        public override bool AfterInit() {
            AddReceiveCallBack(EGameMsgID.EGMI_ACK_GET_OBJECT_LIST, OnGetObjectList);
            return true;
        }


        public override bool BeforeShut() {
            return true;
        }

        public override bool Shut() {
            return true;
        }

        public override bool Execute() {
            return true;
        }

        public override void RegisterPropertyCallback(NFGUID self, string strPropertyName,
            NFIProperty.PropertyEventHandler handler) {
            NFIObject xGameObject = GetObject(self);
            if (null != xGameObject) {
                xGameObject.GetPropertyManager().RegisterCallback(strPropertyName, handler);
            }
        }

        public override void RegisterRecordCallback(NFGUID self, string strRecordName,
            NFIRecord.RecordEventHandler handler) {
            NFIObject xGameObject = GetObject(self);
            if (null != xGameObject) {
                xGameObject.GetRecordManager().RegisterCallback(strRecordName, handler);
            }
        }

        public override void RegisterClassCallBack(string strClassName, NFIObject.ClassEventHandler handler) {
            if (mhtClassHandleDel.ContainsKey(strClassName)) {
                ClassHandleDel xHandleDel = (ClassHandleDel) mhtClassHandleDel[strClassName];
                xHandleDel.AddDel(handler);
            } else {
                ClassHandleDel xHandleDel = new ClassHandleDel();
                xHandleDel.AddDel(handler);
                mhtClassHandleDel[strClassName] = xHandleDel;
            }
        }

        public override void RegisterEventCallBack(NFGUID self, int nEventID, NFIEvent.EventHandler handler) {
            NFIObject xGameObject = GetObject(self);
            if (null != xGameObject) {
                //xGameObject.GetEventManager().RegisterCallback(nEventID, handler, valueList);
            }
        }

        public override NFIObject GetObject(NFGUID ident) {
            if (null != ident && mhtObject.ContainsKey(ident)) {
                return (NFIObject) mhtObject[ident];
            }

            return null;
        }

        public override NFIObject CreateObject(NFGUID self, int nContainerID, int nGroupID, string strClassName,
            string strConfigIndex, NFIDataList arg) {
            if (!mhtObject.ContainsKey(self)) {
                NFIObject xNewObject = new NFCObject(self, nContainerID, nGroupID, strClassName, strConfigIndex);
                mhtObject.Add(self, xNewObject);

                NFCDataList varConfigID = new NFCDataList();
                varConfigID.AddString(strConfigIndex);
                xNewObject.GetPropertyManager().AddProperty("ConfigID", varConfigID);

                NFCDataList varConfigClass = new NFCDataList();
                varConfigClass.AddString(strClassName);
                xNewObject.GetPropertyManager().AddProperty("ClassName", varConfigClass);

                if (arg.Count() % 2 == 0) {
                    for (int i = 0; i < arg.Count() - 1; i += 2) {
                        string strPropertyName = arg.StringVal(i);
                        NFIDataList.VARIANT_TYPE eType = arg.GetType(i + 1);
                        switch (eType) {
                            case NFIDataList.VARIANT_TYPE.VTYPE_INT: {
                                NFIDataList xDataList = new NFCDataList();
                                xDataList.AddInt(arg.IntVal(i + 1));
                                xNewObject.GetPropertyManager().AddProperty(strPropertyName, xDataList);
                            }
                                break;
                            case NFIDataList.VARIANT_TYPE.VTYPE_FLOAT: {
                                NFIDataList xDataList = new NFCDataList();
                                xDataList.AddFloat(arg.FloatVal(i + 1));
                                xNewObject.GetPropertyManager().AddProperty(strPropertyName, xDataList);
                            }
                                break;
                            case NFIDataList.VARIANT_TYPE.VTYPE_STRING: {
                                NFIDataList xDataList = new NFCDataList();
                                xDataList.AddString(arg.StringVal(i + 1));
                                xNewObject.GetPropertyManager().AddProperty(strPropertyName, xDataList);
                            }
                                break;
                            case NFIDataList.VARIANT_TYPE.VTYPE_OBJECT: {
                                NFIDataList xDataList = new NFCDataList();
                                xDataList.AddObject(arg.ObjectVal(i + 1));
                                xNewObject.GetPropertyManager().AddProperty(strPropertyName, xDataList);
                            }
                                break;
                            case NFIDataList.VARIANT_TYPE.VTYPE_VECTOR2: {
                                NFIDataList xDataList = new NFCDataList();
                                xDataList.AddVector2(arg.Vector2Val(i + 1));
                                xNewObject.GetPropertyManager().AddProperty(strPropertyName, xDataList);
                            }
                                break;
                            case NFIDataList.VARIANT_TYPE.VTYPE_VECTOR3: {
                                NFIDataList xDataList = new NFCDataList();
                                xDataList.AddVector3(arg.Vector3Val(i + 1));
                                xNewObject.GetPropertyManager().AddProperty(strPropertyName, xDataList);
                            }
                                break;
                            default:
                                break;
                        }
                    }
                }

                InitProperty(self, strClassName);
                InitRecord(self, strClassName);

                ClassHandleDel xHandleDel = (ClassHandleDel) mhtClassHandleDel[strClassName];
                if (null != xHandleDel && null != xHandleDel.GetHandler()) {
                    NFIObject.ClassEventHandler xHandlerList = xHandleDel.GetHandler();
                    xHandlerList(self, nContainerID, nGroupID, NFIObject.CLASS_EVENT_TYPE.OBJECT_CREATE, strClassName,
                        strConfigIndex);
                    xHandlerList(self, nContainerID, nGroupID, NFIObject.CLASS_EVENT_TYPE.OBJECT_LOADDATA, strClassName,
                        strConfigIndex);
                    xHandlerList(self, nContainerID, nGroupID, NFIObject.CLASS_EVENT_TYPE.OBJECT_CREATE_FINISH,
                        strClassName, strConfigIndex);
                }

                //NFCLog.Instance.Log(NFCLog.LOG_LEVEL.DEBUG, "Create object: " + self.ToString() + " ClassName: " + strClassName + " SceneID: " + nContainerID + " GroupID: " + nGroupID);
                return xNewObject;
            }

            return null;
        }


        public override bool DestroyObject(NFGUID self) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];

                string strClassName = xGameObject.ClassName();

                ClassHandleDel xHandleDel = (ClassHandleDel) mhtClassHandleDel[strClassName];
                if (null != xHandleDel && null != xHandleDel.GetHandler()) {
                    NFIObject.ClassEventHandler xHandlerList = xHandleDel.GetHandler();
                    xHandlerList(self, xGameObject.ContainerID(), xGameObject.GroupID(),
                        NFIObject.CLASS_EVENT_TYPE.OBJECT_DESTROY, xGameObject.ClassName(), xGameObject.ConfigIndex());
                }
                mhtObject.Remove(self);

                //NFCLog.Instance.Log(NFCLog.LOG_LEVEL.DEBUG, "Destroy object: " + self.ToString() + " ClassName: " + strClassName + " SceneID: " + xGameObject.ContainerID() + " GroupID: " + xGameObject.GroupID());

                return true;
            }

            return false;
        }

        public override bool FindProperty(NFGUID self, string strPropertyName) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.FindProperty(strPropertyName);
            }

            return false;
        }

        public override bool SetPropertyInt(NFGUID self, string strPropertyName, Int64 nValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetPropertyInt(strPropertyName, nValue);
            }

            return false;
        }

        public override bool SetPropertyFloat(NFGUID self, string strPropertyName, double fValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetPropertyFloat(strPropertyName, fValue);
            }

            return false;
        }

        public override bool SetPropertyString(NFGUID self, string strPropertyName, string strValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetPropertyString(strPropertyName, strValue);
            }

            return false;
        }

        public override bool SetPropertyObject(NFGUID self, string strPropertyName, NFGUID objectValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetPropertyObject(strPropertyName, objectValue);
            }

            return false;
        }

        public override bool SetPropertyVector2(NFGUID self, string strPropertyName, NFVector2 objectValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetPropertyVector2(strPropertyName, objectValue);
            }

            return false;
        }

        public override bool SetPropertyVector3(NFGUID self, string strPropertyName, NFVector3 objectValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetPropertyVector3(strPropertyName, objectValue);
            }

            return false;
        }

        public override Int64 QueryPropertyInt(NFGUID self, string strPropertyName) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryPropertyInt(strPropertyName);
            }

            return 0;
        }

        public override double QueryPropertyFloat(NFGUID self, string strPropertyName) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryPropertyFloat(strPropertyName);
            }

            return 0.0;
        }

        public override string QueryPropertyString(NFGUID self, string strPropertyName) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryPropertyString(strPropertyName);
            }

            return "";
        }

        public override NFGUID QueryPropertyObject(NFGUID self, string strPropertyName) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryPropertyObject(strPropertyName);
            }

            return new NFGUID();
        }

        public override NFVector2 QueryPropertyVector2(NFGUID self, string strPropertyName) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryPropertyVector2(strPropertyName);
            }

            return new NFVector2();
        }

        public override NFVector3 QueryPropertyVector3(NFGUID self, string strPropertyName) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryPropertyVector3(strPropertyName);
            }

            return new NFVector3();
        }


        public override NFIRecord FindRecord(NFGUID self, string strRecordName) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.GetRecordManager().GetRecord(strRecordName);
            }

            return null;
        }


        public override bool SetRecordInt(NFGUID self, string strRecordName, int nRow, int nCol, Int64 nValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetRecordInt(strRecordName, nRow, nCol, nValue);
            }

            return false;
        }

        public override bool SetRecordFloat(NFGUID self, string strRecordName, int nRow, int nCol, double fValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetRecordFloat(strRecordName, nRow, nCol, fValue);
            }

            return false;
        }


        public override bool SetRecordString(NFGUID self, string strRecordName, int nRow, int nCol, string strValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetRecordString(strRecordName, nRow, nCol, strValue);
            }

            return false;
        }

        public override bool SetRecordObject(NFGUID self, string strRecordName, int nRow, int nCol,
            NFGUID objectValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetRecordObject(strRecordName, nRow, nCol, objectValue);
            }

            return false;
        }

        public override bool SetRecordVector2(NFGUID self, string strRecordName, int nRow, int nCol,
            NFVector2 objectValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetRecordVector2(strRecordName, nRow, nCol, objectValue);
            }

            return false;
        }

        public override bool SetRecordVector3(NFGUID self, string strRecordName, int nRow, int nCol,
            NFVector3 objectValue) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.SetRecordVector3(strRecordName, nRow, nCol, objectValue);
            }

            return false;
        }


        public override Int64 QueryRecordInt(NFGUID self, string strRecordName, int nRow, int nCol) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryRecordInt(strRecordName, nRow, nCol);
            }

            return 0;
        }

        public override double QueryRecordFloat(NFGUID self, string strRecordName, int nRow, int nCol) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryRecordFloat(strRecordName, nRow, nCol);
            }

            return 0.0;
        }

        public override string QueryRecordString(NFGUID self, string strRecordName, int nRow, int nCol) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryRecordString(strRecordName, nRow, nCol);
            }

            return "";
        }

        public override NFGUID QueryRecordObject(NFGUID self, string strRecordName, int nRow, int nCol) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryRecordObject(strRecordName, nRow, nCol);
            }

            return new NFGUID();
        }

        public override NFVector2 QueryRecordVector2(NFGUID self, string strRecordName, int nRow, int nCol) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryRecordVector2(strRecordName, nRow, nCol);
            }

            return new NFVector2();
        }

        public override NFVector3 QueryRecordVector3(NFGUID self, string strRecordName, int nRow, int nCol) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                return xGameObject.QueryRecordVector3(strRecordName, nRow, nCol);
            }

            return new NFVector3();
        }

        public override NFIDataList GetObjectList() {
            NFIDataList varData = new NFCDataList();
            foreach (KeyValuePair<NFGUID, NFIObject> kv in mhtObject) {
                varData.AddObject(kv.Key);
            }

            return varData;
        }

        public override int FindRecordRow(NFGUID self, string strRecordName, int nCol, int nValue,
            ref NFIDataList xDatalist) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                NFIRecord xRecord = xGameObject.GetRecordManager().GetRecord(strRecordName);
                if (null != xRecord) {
                    return xRecord.FindInt(nCol, nValue, ref xDatalist);
                }
            }

            return -1;
        }

        public override int FindRecordRow(NFGUID self, string strRecordName, int nCol, double fValue,
            ref NFIDataList xDatalist) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                NFIRecord xRecord = xGameObject.GetRecordManager().GetRecord(strRecordName);
                if (null != xRecord) {
                    return xRecord.FindFloat(nCol, fValue, ref xDatalist);
                }
            }

            return -1;
        }

        public override int FindRecordRow(NFGUID self, string strRecordName, int nCol, string strValue,
            ref NFIDataList xDatalist) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                NFIRecord xRecord = xGameObject.GetRecordManager().GetRecord(strRecordName);
                if (null != xRecord) {
                    return xRecord.FindString(nCol, strValue, ref xDatalist);
                }
            }

            return -1;
        }

        public override int FindRecordRow(NFGUID self, string strRecordName, int nCol, NFGUID nValue,
            ref NFIDataList xDatalist) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                NFIRecord xRecord = xGameObject.GetRecordManager().GetRecord(strRecordName);
                if (null != xRecord) {
                    return xRecord.FindObject(nCol, nValue, ref xDatalist);
                }
            }

            return -1;
        }

        public override int FindRecordRow(NFGUID self, string strRecordName, int nCol, NFVector2 nValue,
            ref NFIDataList xDatalist) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                NFIRecord xRecord = xGameObject.GetRecordManager().GetRecord(strRecordName);
                if (null != xRecord) {
                    return xRecord.FindVector2(nCol, nValue, ref xDatalist);
                }
            }

            return -1;
        }

        public override int FindRecordRow(NFGUID self, string strRecordName, int nCol, NFVector3 nValue,
            ref NFIDataList xDatalist) {
            if (mhtObject.ContainsKey(self)) {
                NFIObject xGameObject = (NFIObject) mhtObject[self];
                NFIRecord xRecord = xGameObject.GetRecordManager().GetRecord(strRecordName);
                if (null != xRecord) {
                    return xRecord.FindVector3(nCol, nValue, ref xDatalist);
                }
            }

            return -1;
        }

        void InitProperty(NFGUID self, string strClassName) {
            NFILogicClass xLogicClass = mxLogicClassModule.GetElement(strClassName);
            NFIDataList xDataList = xLogicClass.GetPropertyManager().GetPropertyList();
            for (int i = 0; i < xDataList.Count(); ++i) {
                string strPropertyName = xDataList.StringVal(i);
                NFIProperty xProperty = xLogicClass.GetPropertyManager().GetProperty(strPropertyName);

                NFIObject xObject = GetObject(self);
                NFIPropertyManager xPropertyManager = xObject.GetPropertyManager();

                NFIProperty property = xPropertyManager.AddProperty(strPropertyName, xProperty.GetData());
                //if property==null ,means this property alreay exist in manager
                if (property != null) {
                    property.SetUpload(xProperty.GetUpload());
                }
            }
        }

        void InitRecord(NFGUID self, string strClassName) {
            NFILogicClass xLogicClass = mxLogicClassModule.GetElement(strClassName);
            NFIDataList xDataList = xLogicClass.GetRecordManager().GetRecordList();
            for (int i = 0; i < xDataList.Count(); ++i) {
                string strRecordyName = xDataList.StringVal(i);
                NFIRecord xRecord = xLogicClass.GetRecordManager().GetRecord(strRecordyName);

                NFIObject xObject = GetObject(self);
                NFIRecordManager xRecordManager = xObject.GetRecordManager();

                NFIRecord record = xRecordManager.AddRecord(strRecordyName, xRecord.GetRows(), xRecord.GetColsData());
                if (record != null) {
                    record.SetUpload(xRecord.GetUpload());
                }
            }
        }

        private Dictionary<NFGUID, NFIObject> mhtObject;
        private Dictionary<string, ClassHandleDel> mhtClassHandleDel;
        private NFIElementModule mxElementModule;
        private NFILogicClassModule mxLogicClassModule;

        class ClassHandleDel {
            public ClassHandleDel() {
                mhtHandleDelList = new Dictionary<NFIObject.ClassEventHandler, string>();
            }

            public void AddDel(NFIObject.ClassEventHandler handler) {
                if (!mhtHandleDelList.ContainsKey(handler)) {
                    mhtHandleDelList.Add(handler, handler.ToString());
                    mHandleDel += handler;
                }
            }

            public NFIObject.ClassEventHandler GetHandler() {
                return mHandleDel;
            }

            private NFIObject.ClassEventHandler mHandleDel;
            private Dictionary<NFIObject.ClassEventHandler, string> mhtHandleDelList;
        }

        public void AddReceiveCallBack(EGameMsgID id, NFCMessageDispatcher.MessageHandler netHandler) {
            NFCNetDispatcher.Instance().AddReceiveCallBack((UInt16) id, netHandler);
        }

        private List<NFIObject> mGroupPlayerList = new List<NFIObject>();

        public void GetGroupObjectList(int sceneId, int groupId, Ident objId) {
            ReqGroupObjectList xData = new ReqGroupObjectList {
                id = objId,
                sceneid = sceneId,
                groupid = groupId
            };

            MemoryStream stream = new MemoryStream();
            Serializer.Serialize(stream, xData);

            NetLogic.Instance().SendToServerByPb(EGameMsgID.EGMI_REQ_GET_OBJECT_LIST, stream);
        }

        private void OnGetObjectList(ushort id, MemoryStream stream) {
            MsgBase xMsg = new MsgBase();
            xMsg = Serializer.Deserialize<MsgBase>(stream);

            AckPlayerEntryList xData = new AckPlayerEntryList();
            xData = Serializer.Deserialize<AckPlayerEntryList>(new MemoryStream(xMsg.msg_data));

            for (int i = 0; i < xData.object_list.Count; ++i) {
                PlayerEntryInfo xInfo = xData.object_list[i];

                NFIDataList var = new NFCDataList();
                var.AddString("X");
                var.AddFloat(xInfo.x);
                var.AddString("Y");
                var.AddFloat(xInfo.z);
                var.AddString("Z");
                var.AddFloat(xInfo.y);

                NFIObject xGO = CreateObject(LogicBase.PBToNF(xInfo.object_guid), xInfo.scene_id, 0,
                    Encoding.Default.GetString(xInfo.class_id),
                    Encoding.Default.GetString(xInfo.config_id), var);
//            var.AddObject(PBToNF(xInfo.object_guid));
//
//            DoEvent((int) Event.OtherClientShow, var);

                if (null == xGO) {
                    continue;
                }

                mGroupPlayerList.Add(xGO);
            }
        }
    }
}