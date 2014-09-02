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
using System.Linq;
using System.Text;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpPro.Properties;
using NUnit.Framework;
using Parser = Lextm.SharpSnmpPro.Mib.Parser2;

#pragma warning disable 1591
namespace Lextm.SharpSnmpPro.Mib.Tests
{
    [TestFixture]
    public class ObjectRegistryTestFixture
    {
        [Test]
        public void TestTypeValidation()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.IANAifType_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.IF_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.Test), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.CISCO_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.CISCO_TC), collector));
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
            Assert.IsTrue(obj.ResolvedSyntax is TypeAssignment);
            var type = obj.ResolvedSyntax as TypeAssignment;
            Assert.IsTrue(type.BaseType is TextualConventionMacro);
            Assert.AreEqual("DisplayString", type.Name);
            var macro = type.BaseType as TextualConventionMacro;
            Assert.IsTrue(macro.BaseType is OctetStringType);

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
            var entityTruthValue = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity2").DisplayEntity;
            var truthValueName = new StringBuilder();
            entityTruthValue.ResolvedSyntax.Append(truthValueName);
            Assert.AreEqual("TruthValue ::= TEXTUAL-CONVENTION\r\nSTATUS current\r\nSYNTAX INTEGER { true(1), false(2) }", truthValueName.ToString());
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
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            Assert.IsTrue(registry.ValidateTable(table));
            Assert.IsFalse(registry.ValidateTable(entry));
            Assert.IsFalse(registry.ValidateTable(unknown));
        }

        [Test]
        public void TestGetTextualFrom()
        {
            var oid = new uint[] { 1 };
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            string result = registry.Translate(oid);
            Assert.AreEqual("::iso", result);
        }
        [Test]
        public void TestGetTextualForm()
        {
            var oid2 = new uint[] { 1, 3, 6, 1, 2, 1, 10 };
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            string result2 = registry.Translate(oid2);
            Assert.AreEqual("SNMPv2-SMI::transmission", result2);
        }

        [Test]
        public void TestSNMPv2MIBTextual()
        {
            var oid = new uint[] { 1, 3, 6, 1, 2, 1, 1 };
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            string result = registry.Translate(oid);
            Assert.AreEqual("SNMPv2-MIB::system", result);
        }

        [Test]
        public void TestSNMPv2TMTextual()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            uint[] old = registry.Translate("SNMPv2-SMI::snmpDomains");
            string result = registry.Translate(ObjectIdentifier.AppendTo(old, 1));
            Assert.AreEqual("SNMPv2-TM::snmpUDPDomain", result);
        }

        [Test]
        public void TestIso()
        {
            var expected = new uint[] { 1 };
            const string textual = "::iso";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            uint[] result = registry.Translate(textual);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestTransmission()
        {
            var expected = new uint[] { 1, 3, 6, 1, 2, 1, 10 };
            const string textual = "SNMPv2-SMI::transmission";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            uint[] result = registry.Translate(textual);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestZeroDotZero()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            Assert.AreEqual(new uint[] { 0 }, registry.Translate("::ccitt"));
            const string textual = "SNMPv2-SMI::zeroDotZero";
            var expected = new uint[] { 0, 0 };
            Assert.AreEqual(textual, registry.Translate(expected));
            Assert.AreEqual(expected, registry.Translate(textual));
        }

        [Test]
        public void TestSNMPv2MIBNumerical()
        {
            var expected = new uint[] { 1, 3, 6, 1, 2, 1, 1 };
            const string textual = "SNMPv2-MIB::system";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            uint[] result = registry.Translate(textual);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestSNMPv2TMNumerical()
        {
            var expected = new uint[] { 1, 3, 6, 1, 6, 1, 1 };
            const string textual = "SNMPv2-TM::snmpUDPDomain";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            uint[] result = registry.Translate(textual);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestsysORTable()
        {
            const string name = "SNMPv2-MIB::sysORTable";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            uint[] id = registry.Translate(name);
#if !TRIAL
            Assert.IsTrue(registry.IsTableId(id));
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
            var expected = new uint[] { 1, 3, 6, 1, 2, 1, 1, 9, 0 };
            const string name = "SNMPv2-MIB::sysORTable.0";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            uint[] id = registry.Translate(name);
            Assert.AreEqual(expected, id);
        }

        [Test]
        public void TestsysORTable0Reverse()
        {
            var id = new uint[] { 1, 3, 6, 1, 2, 1, 1, 9, 0 };
            const string expected = "SNMPv2-MIB::sysORTable.0";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            string value = registry.Translate(id);
            Assert.AreEqual(expected, value);
        }

        [Test]
        public void TestsnmpMIB()
        {
            const string name = "SNMPv2-MIB::snmpMIB";
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Refresh();
            uint[] id = registry.Translate(name);
#if !TRIAL
            Assert.IsFalse(registry.IsTableId(id));
#endif
        }

        [Test]
        public void TestActona()
        {
            const string name = "ACTONA-ACTASTOR-MIB::actona";
            var collector = new ErrorRegistry();
            var modules = Parser.Compile(new MemoryStream(Resources.ACTONA_ACTASTOR_MIB), collector);
            var registry = new SimpleObjectRegistry();
            registry.Tree.Collector = collector;            
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
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
            registry.Import(Parser.Compile(new MemoryStream(Resources.RFC_1212), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.RFC1155_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.RFC1213_MIB1), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.IEEE802DOT11_MIB), collector));
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
            registry.Import(Parser.Compile(new MemoryStream(Resources.RFC_1212), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.RFC1155_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.RFC1213_MIB1), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.RFC_1215), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TM), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.ALLIEDTELESYN_MIB), collector));
            registry.Refresh();

            Assert.AreEqual(1, collector.Errors.Count);
        }
        // ReSharper restore InconsistentNaming
    }
}
#pragma warning restore 1591