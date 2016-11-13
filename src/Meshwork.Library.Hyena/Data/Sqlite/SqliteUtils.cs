//
// SqliteUtils.cs
//
// Author:
//   Scott Peterson  <lunchtimemama@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text;
using Mono.Data.Sqlite;

namespace Meshwork.Library.Hyena.Data.Sqlite
{
    internal static class SqliteUtils
    {
        public static string GetType (Type type)
        {
            if (type == typeof (string)) {
                return "TEXT";
            }
            if (type == typeof (int) || type == typeof (long) || type == typeof (bool)
                || type == typeof (DateTime) || type == typeof (TimeSpan) || type.IsEnum) {
                return "INTEGER";
            }
            if (type == typeof (byte[])) {
                return "BLOB";
            }
            throw new Exception (string.Format (
                "The type {0} cannot be bound to a database column.", type.Name));
        }
        
        public static object ToDbFormat (Type type, object value)
        {
            if (type == typeof (string)) {
                // Treat blank strings or strings with only whitespace as null
                return value == null || string.IsNullOrEmpty (((string)value).Trim ())
                    ? null
                    : value;
            }
            if (type == typeof (DateTime)) {
                return DateTime.MinValue.Equals ((DateTime)value)
                    ? (object)null
                    : DateTimeUtil.FromDateTime ((DateTime)value);
            }
            if (type == typeof (TimeSpan)) {
                return TimeSpan.MinValue.Equals ((TimeSpan)value)
                    ? (object)null
                    : ((TimeSpan)value).TotalMilliseconds;
            }
            if (type.IsEnum) {
                return Convert.ChangeType (value, Enum.GetUnderlyingType (type));
            }
            if (type == typeof (bool)) {
                return ((bool)value) ? 1 : 0;
            }

            return value;
        }
        
        public static object FromDbFormat (Type type, object value)
        {
            if (Convert.IsDBNull (value))
                value = null;
            
            if (type == typeof (DateTime)) {
                return value == null
                    ? DateTime.MinValue
                    : DateTimeUtil.ToDateTime (Convert.ToInt64 (value));
            }
            if (type == typeof (TimeSpan)) {
                return value == null
                    ? TimeSpan.MinValue
                    : TimeSpan.FromMilliseconds (Convert.ToInt64 (value));
            }
            if (value == null)
            {
                if (type.IsValueType) {
                    return Activator.CreateInstance (type);
                }
                return null;
            }
            if (type.IsEnum) {
                return Enum.ToObject (type, value);
            }
            if (type == typeof (bool)) {
                return ((long)value == 1);
            }
            return Convert.ChangeType (value, type);
        }
        
        public static string BuildColumnSchema (string type, string name, string default_value,
            DatabaseColumnConstraints constraints)
        {
            var builder = new StringBuilder ();
            builder.Append (name);
            builder.Append (' ');
            builder.Append (type);
            if ((constraints & DatabaseColumnConstraints.NotNull) > 0) {
                builder.Append (" NOT NULL");
            }
            if ((constraints & DatabaseColumnConstraints.Unique) > 0) {
                builder.Append (" UNIQUE");
            }
            if ((constraints & DatabaseColumnConstraints.PrimaryKey) > 0) {
                builder.Append (" PRIMARY KEY");
            }
            if (default_value != null) {
                builder.Append (" DEFAULT ");
                builder.Append (default_value);
            }
            return builder.ToString ();
        }
    }
        
    [SqliteFunction (Name = "HYENA_COLLATION_KEY", FuncType = FunctionType.Scalar, Arguments = 1)]
    internal class CollationKeyFunction : SqliteFunction
    {
        public override object Invoke (object[] args)
        {
            return StringUtil.SortKey (args[0] as string);
        }
    }
    
    [SqliteFunction (Name = "HYENA_SEARCH_KEY", FuncType = FunctionType.Scalar, Arguments = 1)]
    internal class SearchKeyFunction : SqliteFunction
    {
        public override object Invoke (object[] args)
        {
            return StringUtil.SearchKey (args[0] as string);
        }
    }
}
