using System.ComponentModel;
using System.Net;
using System.Reflection;

static class Program
{
/// <summary>
/// Cracks command line arguments
/// </summary>
/// <param name="defaultname">The key name of the default parameter</param>
/// <param name="args">The args passed to the entry point</param>
/// <param name="required">A collection of strings that indicate the required arguments</param>
/// <param name="arguments">A dictionary of arguments, with empty instances of various types for the values which specify the "type" of the argument</param>
/// <exception cref="ArgumentException"></exception>
/// <exception cref="InvalidProgramException"></exception>
public static void CrackArguments(string defaultname, string[] args, ICollection<string> required, IDictionary<string, object> arguments)
{

	var argi = 0;
	if (!string.IsNullOrEmpty(defaultname))
	{
		if (args.Length == 0 || args[0][0] == '/')
		{
			if (required.Contains(defaultname))
				throw new ArgumentException(string.Format("<{0}> must be specified.", defaultname));
		}
		else
		{
			var o = arguments[defaultname];
			Type et = o.GetType();
			var isarr = et.IsArray;
			MethodInfo coladd = null;
			MethodInfo parse = null;
			if (isarr)
			{
				et = et.GetElementType();
			}
			else
			{
				foreach (var it in et.GetInterfaces())
				{
					if (!it.IsGenericType) continue;
					var tdef = it.GetGenericTypeDefinition();
					if (typeof(ICollection<>) == tdef)
					{

						et = et.GenericTypeArguments[0];
						coladd = it.GetMethod("Add", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, new Type[] { et });

					}
				}
			}
			TypeConverter conv = TypeDescriptor.GetConverter(et);
			if (conv != null)
			{
				if (!conv.CanConvertFrom(typeof(string)))
				{
					conv = null;
				}
			}
			if (conv == null && !isarr && coladd == null)
			{
				var bt = o.GetType();
				while (parse == null && bt != null)
				{
					try
					{
						parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public);
					}
					catch (AmbiguousMatchException)
					{
						parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(string) });

					}
					bt = bt.BaseType;
				}
			}
			if (!isarr && coladd == null && !(o is string) && conv == null)
				throw new InvalidProgramException(string.Format("Type for {0} must be string or a collection, array or convertible type", defaultname));

			for (; argi < args.Length; ++argi)
			{
				var arg = args[argi];
				if (arg[0] == '/') break;
				if (isarr)
				{
					var arr = (Array)o;
					var newArr = Array.CreateInstance(et, arr.Length + 1);
					Array.Copy(arr, newArr, newArr.Length - 1);
					object v;
					if (conv == null)
					{
						v = arg;
					}
					else
					{
						v = conv.ConvertFromInvariantString(arg);
					}
					newArr.SetValue(v, newArr.Length - 1);
					arguments[defaultname] = newArr;
					o = newArr;
				}
				else if (coladd != null)
				{
					object v;
					if (conv == null)
					{
						v = arg;
					}
					else
					{

						v = conv.ConvertFromInvariantString(arg);
					}
					coladd.Invoke(o, new object[] { v });
				}
				else if ("" == (string)o)
				{
					arguments[defaultname] = arg;
				}
				else if (conv != null)
				{
					arguments[defaultname] = conv.ConvertFromInvariantString(arg);
				}
				else if (parse != null)
				{
					arguments[defaultname] = parse.Invoke(o, new object[] { arg });
				}
				else
					throw new ArgumentException(string.Format("Only one <{0}> value may be specified.", defaultname));
			}
		}
	}
	for (; argi < args.Length; ++argi)
	{
		var arg = args[argi];
		if (string.IsNullOrWhiteSpace(arg) || arg[0] != '/')
		{
			throw new ArgumentException(string.Format("Expected switch instead of {0}", arg));
		}
		arg = arg.Substring(1);
		if (!char.IsLetterOrDigit(arg, 0))
			throw new ArgumentException("Invalid switch /{0}", arg);
		object o;
		if (!arguments.TryGetValue(arg, out o))
		{
			throw new InvalidProgramException(string.Format("Unknown switch /{0}", arg));
		}
		Type et = o.GetType();
		var isarr = et.IsArray;
		MethodInfo coladd = null;
		MethodInfo parse = null;
		var isbool = o is bool;
		var isstr = o is string;
		if (isarr)
		{
			et = et.GetElementType();
		}
		else
		{
			foreach (var it in et.GetInterfaces())
			{
				if (!it.IsGenericType) continue;
				var tdef = it.GetGenericTypeDefinition();
				if (typeof(ICollection<>) == tdef)
				{
					et = et.GenericTypeArguments[0];
					coladd = it.GetMethod("Add", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, new Type[] { et });

				}
			}

		}
		TypeConverter conv = TypeDescriptor.GetConverter(et);
		if (conv != null)
		{
			if (!conv.CanConvertFrom(typeof(string)))
			{
				conv = null;
			}
		}
		if (conv == null && !isarr && coladd == null)
		{
			var bt = o.GetType();
			while (parse == null && bt != null)
			{
				try
				{
					parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public);
				}
				catch (AmbiguousMatchException)
				{
					parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(string) });

				}
				bt = bt.BaseType;
			}
		}
		if (isarr || coladd != null)
		{
			while (++argi < args.Length)
			{
				var sarg = args[argi];
				if (sarg[0] == '/')
					break;
				if (isarr)
				{

					var arr = (Array)o;
					var newArr = Array.CreateInstance(et, arr.Length + 1);
					Array.Copy(newArr, arr, arr.Length - 1);
					object v;
					if (conv == null)
					{
						v = sarg;
					}
					else
					{
						v = conv.ConvertFromInvariantString(sarg);
					}
					newArr.SetValue(v, arr.Length - 1);


				}
				else if (coladd != null)
				{
					object v;
					if (conv == null)
					{
						v = arg;
					}
					else
					{

						v = conv.ConvertFromInvariantString(sarg);
					}
					coladd.Invoke(o, new object[] { v });
				}
			}
		}
		else if (isstr)
		{
			if (argi == args.Length - 1)
				throw new ArgumentException(string.Format("Missing value for /{0}", arg));
			var sarg = args[++argi];
			if ("" == (string)o)
			{
				arguments[arg] = sarg;
			}
			else
				throw new ArgumentException(string.Format("Only one <{0}> value may be specified.", arg));
		}
		else if (isbool)
		{
			if ((bool)o)
			{
				throw new ArgumentException(string.Format("Only one /{0} switch may be specified.", arg));
			}
			arguments[arg] = true;
		}
		else if (conv != null)
		{
			if (argi == args.Length - 1)
				throw new ArgumentException(string.Format("Missing value for /{0}", arg));
			arguments[arg] = conv.ConvertFromInvariantString(args[++argi]);
		}
		else if (parse != null)
		{
			arguments[arg] = parse.Invoke(o, new object[] { args[++argi] });
		}
		else
			throw new InvalidProgramException(string.Format("Type for {0} must be a boolean, a string, a string collection, a string array, or a convertible type", arg));
	}
	foreach (var arg in required)
	{
		if (!arguments.ContainsKey(arg))
		{
			throw new ArgumentException(string.Format("Missing required switch /{0}", arg));
		}
		var o = arguments[arg];
		if (null == o || ((o is string) && ((string)o) == "") || ((o is System.Collections.ICollection) && ((System.Collections.ICollection)o).Count == 0) /*|| ((o is bool) && (!(bool)o))*/)
			throw new ArgumentException(string.Format("Missing required switch /{0}", arg));
	}
}
	static int Main(string[] args)
	{
		var arguments = new Dictionary<string, object>();
		arguments.Add("inputs", new string[0]); // the input files (can be a List<string>)
		arguments.Add("output", ""); // the output file
		arguments.Add("ifstale", false);
		arguments.Add("id", Guid.Empty);
		arguments.Add("ip", IPAddress.None);
		CrackArguments("inputs", args, new string[] { "inputs" }, arguments);
		foreach (var entry in arguments)
		{
			Console.Write(entry.Key + ": ");
			var v = entry.Value;
			if (v is string[])
			{
				var sa = (string[])v;
				Console.WriteLine(string.Join(", ", sa));
			}
			else if (v is ICollection<string>)
			{
				var col = (ICollection<string>)v;
				var delim = "";
				foreach (var item in col)
				{
					Console.Write(delim);
					Console.Write(item);
					delim = ", ";
				}
				Console.WriteLine();
			}
			else
			{
				Console.WriteLine(v);
			}
		}
		return 0;
	}
}