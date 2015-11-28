﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace ExcelInterfaces
{
    public interface IPublicObject
    {
        string Handle { get; set; }
    }

    public class ObjectMissing : ApplicationException
    {
        public ObjectMissing(string handle) :
            base("Object missing : " + handle)
        {
        }
    }

    public static class PublicObject
    {
        public static T This<T>(string handle) where T : IPublicObject
        {
            T publicObject;
            if (!Globals.TryGetItem(handle, out publicObject))
                throw new ObjectMissing(handle);

            return publicObject;
        }

        public static string WriteToXml(this IPublicObject obj)
        {
            var fieldList = new List<FieldInfo>(obj.GetType().GetFields().Where(info => info.IsPublic));
            var typesList = fieldList.Select(field => field.GetValue(obj).GetType()).Distinct().ToList();
            var x = new XmlSerializer(obj.GetType(),typesList.ToArray());
            var sw = new StringWriter();
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            x.Serialize(sw, obj, ns);
            return sw.ToString();
        }
    }

    public static class Globals
    {
        private static readonly Dictionary<string, object> Items = new Dictionary<string, object>();

        private static bool TryGetTypedValue<TKey, TValue, TActual>(
            this IDictionary<TKey, TValue> data,
            TKey key,
            out TActual value) where TActual : TValue
        {
            TValue tmp;
            if (data.TryGetValue(key, out tmp))
            {
                value = (TActual) tmp;
                return true;
            }
            value = default(TActual);
            return false;
        }

        public static string AddItem(string handle, object obj)
        {
            var tHandle = TimestampHandle(handle) + "::" + obj.GetType().Name + "::";
            if (!Items.ContainsKey(tHandle))
                Items.Add(tHandle,obj);

            return tHandle;
        }

        public static string AddItem(string handle, IPublicObject obj)
        {
            var tHandle = TimestampHandle(handle) + "::" + obj.GetType().BaseType?.Name + "::";
            if (!Items.ContainsKey(tHandle))
                Items.Add(tHandle, obj);

            // store timestamped handle
            obj.Handle = tHandle;

            return tHandle;
        }

        public static object GetItem(string handle)
        {
            object obj;
            return TryGetItem(handle, out obj) ? obj : null;
        }

        public static bool TryGetItem<TValue>(string handle,out TValue obj)
        {
            return Items.TryGetTypedValue(handle, out obj);
        }


        private static string TimestampHandle(string handle)
        {
            return handle + "::" + DateTime.Now.ToString("mm:ss.ffff");
        }
    }
}
