using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityHeapEx
{
    public class ObjectList 
    {
        [MenuItem( "Tools/Memory/ObjectList" )]
        public static void DoStuff()
        {
            var l = new ObjectList();
            l.Dump();
        }

        private readonly HashSet<object> seenObjects = new HashSet<object>();
        private StreamWriter writer;

        public void Dump()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var gameAssembly = assemblies.Single(a => a.FullName.Contains("Assembly-CSharp,"));
            var allScripts = UnityEngine.Object.FindObjectsOfType(typeof(MonoBehaviour));

            // used to prevent going through same object twice
            seenObjects.Clear();
            using (writer = new StreamWriter("object_list.txt")) {
				// enumerate all MonoBehaviours 
                // that is, all user scripts on all existing objects.
                foreach (MonoBehaviour o in allScripts) {
                    PrintObject(o, o.name);
                }
            }

            Debug.Log( "OK" );
        }

        private void PrintObject(object o, string name)
        {
            if (o == null) {
                return;
            }

            string objType = "";
            string objDesc = "";
            if (o is UnityEngine.Object) {
                objType = "UNITY_OBJ";
                objDesc = "[" + o.ToString() + "]";
                if (o is UISprite) {
                    var sprite = o as UISprite;
                    objDesc += " [" + sprite.spriteName + "]";
                }
            } else {
                objType+= "OBJ";
            }

            writer.WriteLine("{0}|{1}|{2}|{3}|{4}",
                SecurityElement.Escape(o.GetHashCode().ToString()),
                SecurityElement.Escape(o.GetType().GetFormattedName()),
                SecurityElement.Escape(name),
                SecurityElement.Escape(objType),
                SecurityElement.Escape(objDesc));

            // check object already seen
            if (seenObjects.Contains(o)) {
                return;
            } else {
                seenObjects.Add(o);
            }

            if (o.GetType().IsArray) {
                // array
                if (o.GetType().GetElementType().IsValueType ||
                    o.GetType().GetElementType() == typeof(string)) {
                    return;
                }
                var array = o as Array;
                foreach (var element in array) {
					PrintObject(element, name + "[]");
                }
            } else {
                // object
                foreach (var fieldInfo in o.GetType().EnumerateAllFields()) {
                    PrintField(o, fieldInfo);
                }
            }
        }

        private void PrintField(object root, FieldInfo fieldInfo)
        {
            var v = fieldInfo.GetValue(root);
            if (v == null) {
                return;
            }

            var ftype = v.GetType();
            if (ftype.IsValueType ||
                ftype == typeof(string)) {
                return;
            }

            PrintObject(v, fieldInfo.Name);
        }
    }
}
