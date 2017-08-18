/*****************************************************************
 * Author: Norbert Eder
 * E-Mail: csharp@gmx.at
 * Blog  : http://devtyr.norberteder.com
 * 
 * Copyright © 2010 by Norbert Eder
 * 
 * ***************************************************************/

using System;
using System.Collections.Generic;
using System.Threading;

namespace DevTyr.Mvvm.Messaging
{
    public class Messenger : IMessenger
    {
        private static Messenger instance;
        private static object lockObject = new object();

        private Dictionary<Type, List<ActionIdentifier>> references = new Dictionary<Type,List<ActionIdentifier>>();

        private Messenger() { }

        public static Messenger Instance
        {
            get
            {
                lock (lockObject)
                {
                    if (instance == null)
                        instance = new Messenger();
                    return instance;
                }
            }
        }

        #region IMessenger Members

        public void Register<TNotification>(object recipient, Action<TNotification> action)
        {
            Register<TNotification>(recipient, null, action);
        }

        public void Register<TNotification>(object recipient, string identCode, Action<TNotification> action)
        {
            Type messageType = typeof(TNotification);

            if (!references.ContainsKey(messageType))
                references.Add(messageType, new List<ActionIdentifier>());

            ActionIdentifier actionIdent = new ActionIdentifier();
            actionIdent.Action = new WeakReferenceAction<TNotification>(recipient, action);
            actionIdent.IdentificationCode = identCode;

            references[messageType].Add(actionIdent);
        }

        public void Send<TNotification>(TNotification notification)
        {
            Type type = typeof(TNotification);
            List<ActionIdentifier> typeActionIdentifiers = references[type];
            foreach (ActionIdentifier ai in typeActionIdentifiers)
            {
                IActionParameter actionParameter = ai.Action as IActionParameter;
                if (actionParameter != null)
                    actionParameter.ExecuteWithParameter(notification);
                else
                    ai.Action.Execute();
            }
        }

        public void Send<TNotification>(TNotification notification, string identCode)
        {
            Type type = typeof(TNotification);
            List<ActionIdentifier> typeActionIdentifiers = references[type];
            foreach (ActionIdentifier ai in typeActionIdentifiers)
            {
                if (ai.IdentificationCode == identCode)
                {
                    IActionParameter actionParameter = ai.Action as IActionParameter;
                    if (actionParameter != null)
                        actionParameter.ExecuteWithParameter(notification);
                    else
                        ai.Action.Execute();
                }
            }
        }

        public void Unregister<TNotification>(object recipient)
        {
            Unregister<TNotification>(recipient, null);
        }

        public void Unregister<TNotification>(object recipient, string identCode)
        {
            bool lockTaken = false;

            try
            {
                Monitor.Enter(references, ref lockTaken);
                foreach (Type targetType in references.Keys)
                {
                    foreach (ActionIdentifier wra in references[targetType])
                    {
                        if (wra.Action != null && wra.Action.Target != null && wra.Action.Target.Target == recipient)
                            if (String.IsNullOrEmpty(identCode) || (!String.IsNullOrEmpty(identCode) && !String.IsNullOrEmpty(wra.IdentificationCode) && wra.IdentificationCode.Equals(identCode)))
                                wra.Action.Unload();
                    }
                }
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(references);
            }
        }

        #endregion
    }
}
