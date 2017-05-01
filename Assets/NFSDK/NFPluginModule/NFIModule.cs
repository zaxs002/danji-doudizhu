using UnityEngine;
using System.Collections;

namespace NFSDK
{
    public abstract class NFIModule
    {
        //------------- 接口 -------------------//
        public abstract bool Awake();
        public abstract bool Init();
        public abstract bool AfterInit();
        public abstract bool Execute();
        public abstract bool BeforeShut();
        public abstract bool Shut();
        public T FindModule<T>() where T : NFIModule
        {
            return mPluginManager._FindModule<T>();
        }

        public NFIModule FindModule(string strModuleName)
        {
            return mPluginManager._FindModule(strModuleName);
        }

        public NFIPluginManager mPluginManager;
        public string mName;
    };
}