using System;
using UnityEngine.Events;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Utility functions related to migrating deprecated <see cref="UnityEvent"/> properties.
    /// </summary>
    public static class EventMigrationUtility
    {
        /// <summary>
        /// Migrate the persistent listeners from one <see cref="UnityEvent"/> to another.
        /// The listeners will be removed from the source event, and appended to the destination event.
        /// The scripts of the target of Dynamic listeners still need to be manually updated to match the new event signature.
        /// </summary>
        /// <param name="srcUnityEvent">The source <see cref="SerializedProperty"/> of the <see cref="UnityEvent"/> to move from.</param>
        /// <param name="dstUnityEvent">The destination <see cref="SerializedProperty"/> of the <see cref="UnityEvent"/> to move to.</param>
        [Obsolete("MigrateEvent is marked for deprecation and will be removed in a future version. It is only used for migrating deprecated events.")]
        public static void MigrateEvent(SerializedProperty srcUnityEvent, SerializedProperty dstUnityEvent)
        {
            var srcCalls = srcUnityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
            var dstCalls = dstUnityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
            for (var srcIndex = 0; srcIndex < srcCalls.arraySize; ++srcIndex)
            {
                var dstIndex = dstCalls.arraySize;
                dstCalls.InsertArrayElementAtIndex(dstIndex);
                var srcPersistentCall = srcCalls.GetArrayElementAtIndex(srcIndex);
                var dstPersistentCall = dstCalls.GetArrayElementAtIndex(dstIndex);
                CopyPersistentCall(srcPersistentCall, dstPersistentCall);
            }

            srcCalls.ClearArray();
        }

        static void CopyPersistentCall(SerializedProperty srcPersistentCall, SerializedProperty dstPersistentCall)
        {
            dstPersistentCall.FindPropertyRelative("m_Target").objectReferenceValue = srcPersistentCall.FindPropertyRelative("m_Target").objectReferenceValue;
            var dstTargetAssemblyTypeName = dstPersistentCall.FindPropertyRelative("m_TargetAssemblyTypeName");
            var srcTargetAssemblyTypeName = srcPersistentCall.FindPropertyRelative("m_TargetAssemblyTypeName");
            if (dstTargetAssemblyTypeName != null && srcTargetAssemblyTypeName != null)
                dstTargetAssemblyTypeName.stringValue = srcTargetAssemblyTypeName.stringValue;
            dstPersistentCall.FindPropertyRelative("m_MethodName").stringValue = srcPersistentCall.FindPropertyRelative("m_MethodName").stringValue;
            dstPersistentCall.FindPropertyRelative("m_Mode").intValue = srcPersistentCall.FindPropertyRelative("m_Mode").intValue;
            CopyArgumentCache(srcPersistentCall.FindPropertyRelative("m_Arguments"), dstPersistentCall.FindPropertyRelative("m_Arguments"));
            dstPersistentCall.FindPropertyRelative("m_CallState").intValue = srcPersistentCall.FindPropertyRelative("m_CallState").intValue;
        }

        static void CopyArgumentCache(SerializedProperty srcArguments, SerializedProperty dstArguments)
        {
            dstArguments.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = srcArguments.FindPropertyRelative("m_ObjectArgument").objectReferenceValue;
            dstArguments.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = srcArguments.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue;
            dstArguments.FindPropertyRelative("m_IntArgument").intValue = srcArguments.FindPropertyRelative("m_IntArgument").intValue;
            dstArguments.FindPropertyRelative("m_FloatArgument").floatValue = srcArguments.FindPropertyRelative("m_FloatArgument").floatValue;
            dstArguments.FindPropertyRelative("m_StringArgument").stringValue = srcArguments.FindPropertyRelative("m_StringArgument").stringValue;
            dstArguments.FindPropertyRelative("m_BoolArgument").boolValue = srcArguments.FindPropertyRelative("m_BoolArgument").boolValue;
        }
    }
}
