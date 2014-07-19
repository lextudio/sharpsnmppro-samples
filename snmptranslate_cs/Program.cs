using System;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpPro.Mib;
using System.IO;
using snmptranslate.Properties;

namespace snmptranslate
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine(@"This application takes one parameter.");
                return;
            }

            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            var tree = registry.Tree;
            if (args[0].Contains("::"))
            {
                string name = args[0];
                var oid = registry.Translate(name);
                var id = new ObjectIdentifier(oid);
                Console.WriteLine(id);
            }
            else
            {
                string oid = args[0];
                var o = tree.Search(ObjectIdentifier.Convert(oid));
                string textual = o.AlternativeText;
                Console.WriteLine(textual);
                if (o.GetRemaining().Count == 0)
                {
                    Console.WriteLine(o.Definition.Type.ToString());
                }
            }
        }
    }
}
