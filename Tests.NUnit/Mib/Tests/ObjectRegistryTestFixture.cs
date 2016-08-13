/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/16
 * Time: 21:10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Text;
using Lextm.SharpSnmpLib;
using NUnit.Framework;
using Parser = Lextm.SharpSnmpPro.Mib.Parser2;
using System.Linq;
using System.Reflection;

#pragma warning disable 1591
namespace Lextm.SharpSnmpPro.Mib.Tests
{
    [TestFixture]
    public class ObjectRegistryTestFixture
    {
        private static string GetLocation(string file)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", file);
        }

        [Test]
        public void TestTypeValidation()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("Test.mib"), collector));
            registry.Import(Parser.Compile(GetLocation("CISCO-SMI.mib"), collector));
            registry.Import(Parser.Compile(GetLocation("CISCO-TC.mib"), collector));
            registry.Refresh();

            // Test DisplayString.
            Assert.IsTrue(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString("test")));
            Assert.IsTrue(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString(string.Empty)));
            var item = registry.Tree.Find("SNMPv2-MIB", "sysDescr");
            var entity = item.DisplayEntity;
            Assert.AreEqual("A textual description of the entity.  This value should include the full name and version identification of the system's hardware type, software operating-system, and networking software.", entity.DescriptionFormatted());
            Assert.AreEqual(EntityStatus.Current, entity.Status);
            Assert.AreEqual(string.Empty, entity.Reference);
            
            var obj = entity as IObjectTypeMacro;
            Assert.AreEqual(Access.ReadOnly, obj.MibAccess);
            Assert.AreEqual(SnmpType.OctetString, obj.BaseSyntax);
#if !TRIAL
            Assert.IsTrue(obj.Syntax is UnknownType);
            var type = obj.ResolvedSyntax.GetLastType();
            Assert.IsTrue(type is OctetStringType);

            var name = new StringBuilder();
            obj.ResolvedSyntax.Append(name);
            Assert.AreEqual("DisplayString ::= TEXTUAL-CONVENTION\r\nDISPLAY-HINT \"255a\"\r\nSTATUS current\r\nSYNTAX OCTET STRING", name.ToString());
#endif
            var longStr = new StringBuilder();
            for (int i = 0; i <= 256; i++)
            {
                longStr.Append('t');
            }

            Assert.IsFalse(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString(longStr.ToString())));

            // Test integer
            Assert.IsTrue(registry.Verify("IF-MIB", "ifAdminStatus", new Integer32(2)));
            Assert.AreEqual("down(2)", registry.Decode("IF-MIB", "ifAdminStatus", new Integer32(2)));
            Assert.IsFalse(registry.Verify("IF-MIB", "ifAdminStatus", new Integer32(5)));
#if !TRIAL
            // Test BITS
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity", new OctetString(new byte[] { 0x8 })));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity", new OctetString(new byte[] { 0x8, 0x9 })));
            var bits = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity").DisplayEntity;
            {
                var inner = bits.ResolvedSyntax.GetLastType();
                var inType = inner as OctetStringType;
                Assert.IsNotNull(inType);
                Assert.AreEqual(8, inType.NamedBits.Count);
                var item1 = inType.NamedBits[0] as NamedBit;
                Assert.AreEqual("cos0", item1.Name);
                Assert.AreEqual(0, item1.Number);
                var item2 = inType.NamedBits[1] as NamedBit;
                Assert.AreEqual("cos1", item2.Name);
                Assert.AreEqual(1, item2.Number);
            }
#endif

            // Test TruthValue
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity2", new Integer32(1)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity2", new Integer32(2)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity2", new Integer32(0)));
#if !TRIAL
            var entityTruthValue = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity2").DisplayEntity;
            var truthValueName = new StringBuilder();
            entityTruthValue.ResolvedSyntax.Append(truthValueName);
            Assert.AreEqual("TruthValue ::= TEXTUAL-CONVENTION\r\nSTATUS current\r\nSYNTAX INTEGER { true(1), false(2) }", truthValueName.ToString());
#endif

#if !TRIAL
            {
                var inner = entityTruthValue.ResolvedSyntax.GetLastType();
                var inType = inner as IntegerType;
                Assert.IsNotNull(inType);
                Assert.AreEqual(2, inType.NamedNumberList.Count);
                var item1 = inType.NamedNumberList[0] as NamedNumber;
                Assert.AreEqual("true", item1.Name);
                Assert.AreEqual(1, (item1.Value as NumberLiteralValue).Value);
                var item2 = inType.NamedNumberList[1] as NamedNumber;
                Assert.AreEqual("false", item2.Name);
                Assert.AreEqual(2, (item2.Value as NumberLiteralValue).Value);
            }
#endif

            // Test MacAddress
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.AreEqual("09-09-09-09-09-10", registry.Decode("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x10 })));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9 })));

            // Test RowStatus
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(1)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(2)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(3)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(4)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(5)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(6)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity4", new Integer32(0)));

            // DateAndTime
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.Throws<InvalidOperationException>(() => registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.AreEqual("2004-08-17T15:48:00.0000000-05:00", registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xD4, 0x08, 0x11, 0x0F, 0x30, 0x00, 0x00, 0x2D, 0x05, 0x00 })));
            Assert.AreEqual("2004-08-17T15:48:00.0000000+00:00", registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xD4, 0x08, 0x11, 0x0F, 0x30, 0x00, 0x00 })));
            Assert.AreEqual("1992-05-26T13:30:15.0000000-04:00", registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xC8, 5, 26, 13, 30, 15, 0x00, 0x2D, 0x04, 0x00 })));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9 })));

            // StorageType
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity6", new Integer32(1)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity6", new Integer32(2)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity6", new Integer32(3)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity6", new Integer32(4)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity6", new Integer32(5)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity6", new Integer32(0)));

            // Test TAddress
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity7", new OctetString(new byte[0])));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity7", new OctetString(new byte[] { 0x9 })));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity7", new OctetString(longStr.ToString())));
#if !TRIAL
            // SAP type
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity8", new Integer32(0)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity8", new Integer32(254)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity8", new Integer32(255)));
#endif
            // CountryCode
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[0])));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[] { 0x9 })));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[] { 0x9, 0x9 })));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity9", new OctetString(longStr.ToString())));
#if !TRIAL
            // CountryCodeITU
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(0)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(255)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(256)));
#endif
            // CiscoRowOperStatus
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity11", new Integer32(1)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity11", new Integer32(2)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity11", new Integer32(3)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity11", new Integer32(4)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity11", new Integer32(0)));
#if !TRIAL
            // CiscoPort
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity12", new Integer32(0)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity12", new Integer32(65535)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity12", new Integer32(65536)));
#endif
            // Custom
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity13", new Integer32(0)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity13", new Integer32(30000000)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity13", new Integer32(31010000)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity13", new Integer32(13750000)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity13", new Integer32(14510000)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity13", new Integer32(5850000)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity13", new Integer32(6425100)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity13", new Integer32(7900000)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity13", new Integer32(8401000)));
        }

        /// <summary>
        /// A test case to show how CHOICE type is handled.
        /// </summary>
        /// The object testEntity14 is defined with NetworkAddress syntax.
        /// 
        /// testEntity14 -- test -- OBJECT-TYPE
        ///     SYNTAX      NetworkAddress
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { mytest 14 }
        /// 
        /// This NetworkAddress syntax is of CHOICE type and defined in RFC1155.
        /// 
        ///        NetworkAddress ::=
        ///            CHOICE {
        ///                internet
        ///                    IpAddress
        ///            }
        /// 
        ///        IpAddress ::=
        ///            [APPLICATION 0]          -- in network-byte order
        ///                IMPLICIT OCTET STRING (SIZE (4))
        [Test]
        public void TestChoice()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("Test1.mib"), collector));
            registry.Refresh();

#if !TRIAL
            {
                var choiceValue = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity14").DisplayEntity;
                var resolvedSyntax = choiceValue.ResolvedSyntax;
                var inner = resolvedSyntax.GetLastType();
                Assert.AreEqual("NetworkAddress", inner.Name);
                var inType = inner as ChoiceType;
                Assert.IsNotNull(inType);
                // IMPORTANT: this list only contains one element.
                Assert.AreEqual(1, inType.ElementTypes.Count);
                var item1 = inType.ElementTypes[0] as TaggedElementType;
                Assert.AreEqual("internet", item1.Name);
                var root = item1.Subtype.GetLastType();
                // IMPORTANT: the type of this element is IpAddress.
                Assert.IsTrue(root is IpAddressType);
            }
#endif
        }

        // ReSharper disable InconsistentNaming
        [Test]
        public void TestValidateTable()
        {
            var table = new ObjectIdentifier(new uint[] { 1, 3, 6, 1, 2, 1, 1, 9 });
            var entry = new ObjectIdentifier(new uint[] { 1, 3, 6, 1, 2, 1, 1, 9, 1 });
            var unknown = new ObjectIdentifier(new uint[] { 1, 3, 6, 8, 18579, 111111 });
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector));
            registry.Refresh();
            Assert.IsTrue(registry.ValidateTable(table));
            Assert.IsFalse(registry.ValidateTable(entry));
            Assert.IsFalse(registry.ValidateTable(unknown));
        }

        [Test]
        public void TestGetTextualForms()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector));
            registry.Refresh();
            Assert.AreEqual("::iso", registry.Translate(new uint[] { 1 }));
            Assert.AreEqual(new uint[] { 1 }, registry.Translate("::iso"));

            Assert.AreEqual("SNMPv2-SMI::transmission", registry.Translate(new uint[] { 1, 3, 6, 1, 2, 1, 10 }));
            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 2, 1, 10 }, registry.Translate("SNMPv2-SMI::transmission"));

            Assert.AreEqual("SNMPv2-MIB::system", registry.Translate(new uint[] { 1, 3, 6, 1, 2, 1, 1 }));

            Assert.AreEqual("SNMPv2-TM::snmpUDPDomain", registry.Translate(ObjectIdentifier.AppendTo(registry.Translate("SNMPv2-SMI::snmpDomains"), 1)));
            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 6, 1, 1 }, registry.Translate("SNMPv2-TM::snmpUDPDomain"));

            Assert.AreEqual(new uint[] { 0 }, registry.Translate("::ccitt"));
            Assert.AreEqual("SNMPv2-SMI::zeroDotZero", registry.Translate(new uint[] { 0, 0 }));
            Assert.AreEqual(new uint[] { 0, 0 }, registry.Translate("SNMPv2-SMI::zeroDotZero"));

            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 2, 1, 1 }, registry.Translate("SNMPv2-MIB::system"));
        }

        [Test]
        public void TestsysORTable()
        {
            const string name = "SNMPv2-MIB::sysORTable";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector));
            registry.Refresh();
            uint[] id = registry.Translate(name);
#if !TRIAL
            Assert.IsTrue(registry.ValidateTable(new ObjectIdentifier(id)));
#endif 
            var node = registry.Tree.Find("SNMPv2-MIB", "sysORTable");
            var node1 = registry.Tree.Find("SNMPv2-MIB", "sysOREntry");
            var node2 = registry.Tree.Find("SNMPv2-MIB", "sysORIndex");
            Assert.AreEqual(DefinitionType.Table, node.Type);
            Assert.AreEqual(DefinitionType.Entry, node1.Type);
            Assert.AreEqual(DefinitionType.Column, node2.Type);
        }

        [Test]
        public void TestsysORTable0()
        {
            var id = new uint[] { 1, 3, 6, 1, 2, 1, 1, 9, 0 };
            const string name = "SNMPv2-MIB::sysORTable.0";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector));
            registry.Refresh();
            Assert.AreEqual(id, registry.Translate(name));
            Assert.AreEqual(name, registry.Translate(id));
        }

        [Test]
        public void TestsnmpMIB()
        {
            const string name = "SNMPv2-MIB::snmpMIB";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector));
            registry.Refresh();
            uint[] id = registry.Translate(name);
#if !TRIAL
            Assert.IsFalse(registry.ValidateTable(new ObjectIdentifier(id)));
#endif
        }

        [Test]
        public void TestActona()
        {
            const string name = "ACTONA-ACTASTOR-MIB::actona";
            var collector = new ErrorRegistry();
            var modules = Parser.Compile(GetLocation("ACTONA-ACTASTOR-MIB.mib"), collector);
            var registry = new SimpleObjectRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector));
            registry.Import(modules);
            registry.Refresh();
            uint[] id = registry.Translate(name);

            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 4, 1, 17471 }, id);
            Assert.AreEqual("ACTONA-ACTASTOR-MIB::actona", registry.Translate(id));
        }

        [Test]
        public void TestIEEE802dot11_MIB()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(GetLocation("RFC-1212"), collector));
            registry.Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("RFC1213-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("IEEE802DOT11-MIB.mib"), collector));
            registry.Refresh();

            Assert.AreEqual("IEEE802dot11-MIB::dot11SMTnotification", registry.Translate(new uint[] { 1, 2, 840, 10036, 1, 6 }));
            uint[] id = registry.Translate("IEEE802dot11-MIB::dot11SMTnotification");
            Assert.AreEqual(new uint[] { 1, 2, 840, 10036, 1, 6 }, id);

            const string name1 = "IEEE802dot11-MIB::dot11Disassociate";
            var id1 = new uint[] { 1, 2, 840, 10036, 1, 6, 0, 1 };
            Assert.AreEqual(id1, registry.Translate(name1));
            Assert.AreEqual(name1, registry.Translate(id1));
        }

        [Test]
        public void TestALLIEDTELESYN_MIB()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("RFC-1212"), collector));
            registry.Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("RFC1213-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("ALLIEDTELESYN-MIB.mib"), collector));
            registry.Refresh();

            var o = registry.Tree.Search(ObjectIdentifier.Convert("3.6.1.2.1.25"));
            Assert.IsNull(o.Definition);
            Assert.AreEqual(".3.6.1.2.1.25", o.AlternativeText);
            Assert.AreEqual(".3.6.1.2.1.25", o.Text);
            Assert.AreEqual(1, collector.Errors.Count);
        }

        [Test]
        public void TestHOSTRESOURCES_MIB()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("HOST-RESOURCES-MIB.txt"), collector));
            registry.Refresh();

            Assert.AreEqual(0, collector.Errors.Count);

            var module = registry.Tree.LoadedModules.FirstOrDefault(mod => mod.Name == "HOST-RESOURCES-MIB");
            Assert.AreEqual(83, module.Objects.Count);
        }

        /// <summary>
        /// A test case for complex OID assignments.
        /// </summary>
        /// <remarks>
        /// The OID assignments are defined as below. As ieee802dot1mibs is defined after ieee8021TcMib, the resolution requires special treatment.
        /// 
        /// ieee8021TcMib MODULE-IDENTITY
        ///     LAST-UPDATED "200810150000Z" -- October 15, 2008
        ///     ORGANIZATION "IEEE 802.1 Working Group"
        ///     CONTACT-INFO
        ///         "  WG-URL: http://grouper.ieee.org/groups/802/1/index.html
        ///          WG-EMail: stds-802-1@ieee.org
        /// 
        ///           Contact: David Levi
        ///            Postal: 4655 GREAT AMERICA PARKWAY
        ///                    SANTA CLARA, CALIFORNIA
        ///                    95054
        ///                    USA
        ///               Tel: +1-408-495-5138
        ///            E-mail: dlevi @nortel.com"
        ///     DESCRIPTION
        ///         "Textual conventions used throughout the various IEEE 802.1 MIB
        ///          modules.
        /// 
        ///          Unless otherwise indicated, the references in this MIB
        ///          module are to IEEE 802.1Q-2005 as amended by IEEE 802.1ad,
        ///          IEEE 802.1ak, IEEE 802.1ag and IEEE 802.1ah.
        /// 
        ///          Copyright (C) IEEE.
        ///          This version of this MIB module is part of IEEE802.1Q;
        ///          see the draft itself for full legal notices."
        ///     REVISION     "200810150000Z" -- October 15, 2008
        ///     DESCRIPTION
        ///          "Initial version."
        ///     ::= { org ieee(111) standards-association-numbers-series-standards(2)
        ///           lan-man-stds(802) ieee802dot1(1) 1 1 }
        /// 
        /// ieee802dot1mibs OBJECT IDENTIFIER
        ///     ::= { org ieee(111) standards-association-numbers-series-standards(2)
        ///           lan-man-stds(802) ieee802dot1(1) 1 }
        /// </remarks>
        [Test]
        public void TestObjectIdentifierAssignments()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector));
            registry.Import(Parser.Compile(GetLocation("IEEE8021-TC-MIB.txt"), collector));
            registry.Refresh();

            Assert.AreEqual(0, collector.Errors.Count);

            var module = registry.Tree.LoadedModules.FirstOrDefault(mod => mod.Name == "IEEE8021-TC-MIB");
            Assert.AreEqual(0, module.Objects.Count);
            var child = registry.Translate("IEEE8021-TC-MIB::ieee8021TcMib");
            Assert.AreEqual(".1.3.111.2.802.1.1.1", ObjectIdentifier.Convert(child));
            var parent = registry.Translate("IEEE8021-TC-MIB::ieee802dot1mibs");
            Assert.AreEqual(".1.3.111.2.802.1.1", ObjectIdentifier.Convert(parent));

            // IMPORTANT: assistant OIDs were utilized in 1.1.1 and older releases to support such scenarios. They are no longer required in 1.1.2 and above.
            Assert.IsNull(registry.Tree.Find("IEEE8021-TC-MIB", "ieee802dot1_1"));

            var definition = registry.Tree.Find("IEEE8021-TC-MIB", "ieee802dot1mibs");
            // IMPORTANT: since no more assistant OIDs exists, the textual form is now unique.
            Assert.AreEqual(1, definition.TextualForms.Count);
        }
        // ReSharper restore InconsistentNaming
    }
}
#pragma warning restore 1591