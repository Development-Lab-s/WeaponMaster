using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

namespace DevLib.ModuleSystem
{
    public abstract class NetworkOwner : NetworkBehaviour
    {
        protected Dictionary<Type, IModule> _moduleDict;
        
        protected virtual void Awake()
        {
            _moduleDict = GetComponentsInChildren<IModule>().ToDictionary(m => m.GetType());
            
            InitializeModules();
            AfterInitializeModules();
        }

        protected virtual void InitializeModules()
        {
            foreach(IModule module in _moduleDict.Values)
                module.Initialize(this);
        }

        protected virtual void AfterInitializeModules()
        {
            foreach(IAfterInitModule module in _moduleDict.Values.OfType<IAfterInitModule>())
                module.AfterInit();
        }

        public T GetModule<T>()
        {
            if(_moduleDict.TryGetValue(typeof(T), out IModule module))
                return (T)module;
            
            IModule findModule = _moduleDict.Values.FirstOrDefault(m => m is T);

            if (findModule is T casted)
                return casted;
            
            return default;
        }
    }
}