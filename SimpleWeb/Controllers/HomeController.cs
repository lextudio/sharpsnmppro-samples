using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpPro.Mib.Registry;
using System.IO;
using System.Web.Mvc;
using Lextm.SharpSnmpPro.Mib.Validation;

namespace SimpleWeb.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser2.Compile(GenerateStreamFromString(Properties.Resources.SNMPv2_SMI), collector));
            registry.Import(Parser2.Compile(GenerateStreamFromString(Properties.Resources.SNMPv2_CONF), collector));
            registry.Import(Parser2.Compile(GenerateStreamFromString(Properties.Resources.SNMPv2_TC), collector));
            registry.Import(Parser2.Compile(GenerateStreamFromString(Properties.Resources.SNMPv2_MIB), collector));
            registry.Import(Parser2.Compile(GenerateStreamFromString(Properties.Resources.SNMPv2_TM), collector));
            registry.Refresh();
            var tree = registry.Tree;
            string oid = "1.3.6.1.2.1.1.1.0";
            var o = tree.Search(ObjectIdentifier.Convert(oid));
            string textual = o.AlternativeText;
            ViewBag.Message = string.Format("textual {0}.", textual);

            return View();
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
