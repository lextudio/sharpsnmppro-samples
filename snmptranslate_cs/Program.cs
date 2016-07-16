using System;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpPro.Mib;
using System.IO;
using Parser = Lextm.SharpSnmpPro.Mib.Parser2;
using System.Reflection;

namespace snmptranslate
{
    public static class Program
    {
        private static string GetLocation(string file)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", file);
        }

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
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector));
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
