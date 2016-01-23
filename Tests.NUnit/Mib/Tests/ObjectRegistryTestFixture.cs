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
using Lextm.SharpSnmpPro.Properties;
using Parser = Lextm.SharpSnmpPro.Mib.Parser2;
using System.Linq;
using Xunit;

#pragma warning disable 1591
namespace Lextm.SharpSnmpPro.Mib.Tests
{
    public class ObjectRegistryTestFixture
    {
        [Fact]
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
            Assert.True(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString("test")));
            Assert.True(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString(string.Empty)));
            var item = registry.Tree.Find("SNMPv2-MIB", "sysDescr");
            var entity = item.DisplayEntity;
            Assert.Equal("A textual description of the entity.  This value should include the full name and version identification of the system's hardware type, software operating-system, and networking software.", entity.DescriptionFormatted());
            Assert.Equal(EntityStatus.Current, entity.Status);
            Assert.Equal(string.Empty, entity.Reference);
            
            var obj = entity as IObjectTypeMacro;
            Assert.Equal(Access.ReadOnly, obj.MibAccess);
            Assert.Equal(SnmpType.OctetString, obj.BaseSyntax);
#if !TRIAL
            Assert.True(obj.Syntax is UnknownType);
            var type = obj.ResolvedSyntax.GetLastType();
            Assert.True(type is OctetStringType);

            var name = new StringBuilder();
            obj.ResolvedSyntax.Append(name);
            Assert.Equal("DisplayString ::= TEXTUAL-CONVENTION\r\nDISPLAY-HINT \"255a\"\r\nSTATUS current\r\nSYNTAX OCTET STRING", name.ToString());
#endif
            var longStr = new StringBuilder();
            for (int i = 0; i <= 256; i++)
            {
                longStr.Append('t');
            }

            Assert.False(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString(longStr.ToString())));

            // Test integer
            Assert.True(registry.Verify("IF-MIB", "ifAdminStatus", new Integer32(2)));
            Assert.Equal("down(2)", registry.Decode("IF-MIB", "ifAdminStatus", new Integer32(2)));
            Assert.False(registry.Verify("IF-MIB", "ifAdminStatus", new Integer32(5)));
#if !TRIAL
            // Test BITS
            Assert.True(registry.Verify("TEST-MIB", "testEntity", new OctetString(new byte[] { 0x8 })));
            Assert.False(registry.Verify("TEST-MIB", "testEntity", new OctetString(new byte[] { 0x8, 0x9 })));
            var bits = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity").DisplayEntity;
            {
                var inner = bits.ResolvedSyntax.GetLastType();
                var inType = inner as OctetStringType;
                Assert.NotNull(inType);
                Assert.Equal(8, inType.NamedBits.Count);
                var item1 = inType.NamedBits[0] as NamedBit;
                Assert.Equal("cos0", item1.Name);
                Assert.Equal(0UL, item1.Number);
                var item2 = inType.NamedBits[1] as NamedBit;
                Assert.Equal("cos1", item2.Name);
                Assert.Equal(1UL, item2.Number);
            }
#endif

            // Test TruthValue
            Assert.True(registry.Verify("TEST-MIB", "testEntity2", new Integer32(1)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity2", new Integer32(2)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity2", new Integer32(0)));
#if !TRIAL
            var entityTruthValue = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity2").DisplayEntity;
            var truthValueName = new StringBuilder();
            entityTruthValue.ResolvedSyntax.Append(truthValueName);
            Assert.Equal("TruthValue ::= TEXTUAL-CONVENTION\r\nSTATUS current\r\nSYNTAX INTEGER { true(1), false(2) }", truthValueName.ToString());
#endif

#if !TRIAL
            {
                var inner = entityTruthValue.ResolvedSyntax.GetLastType();
                var inType = inner as IntegerType;
                Assert.NotNull(inType);
                Assert.Equal(2, inType.NamedNumberList.Count);
                var item1 = inType.NamedNumberList[0] as NamedNumber;
                Assert.Equal("true", item1.Name);
                Assert.Equal(1, (item1.Value as NumberLiteralValue).Value);
                var item2 = inType.NamedNumberList[1] as NamedNumber;
                Assert.Equal("false", item2.Name);
                Assert.Equal(2, (item2.Value as NumberLiteralValue).Value);
            }
#endif

            // Test MacAddress
            Assert.True(registry.Verify("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.Equal("09-09-09-09-09-10", registry.Decode("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x10 })));
            Assert.False(registry.Verify("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9 })));

            // Test RowStatus
            Assert.True(registry.Verify("TEST-MIB", "testEntity4", new Integer32(1)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity4", new Integer32(2)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity4", new Integer32(3)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity4", new Integer32(4)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity4", new Integer32(5)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity4", new Integer32(6)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity4", new Integer32(0)));

            // DateAndTime
            Assert.True(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.True(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.Throws<InvalidOperationException>(() => registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.Equal("2004-08-17T15:48:00.0000000-05:00", registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xD4, 0x08, 0x11, 0x0F, 0x30, 0x00, 0x00, 0x2D, 0x05, 0x00 })));
            Assert.Equal("2004-08-17T15:48:00.0000000+00:00", registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xD4, 0x08, 0x11, 0x0F, 0x30, 0x00, 0x00 })));
            Assert.Equal("1992-05-26T13:30:15.0000000-04:00", registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xC8, 5, 26, 13, 30, 15, 0x00, 0x2D, 0x04, 0x00 })));
            Assert.False(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9 })));

            // StorageType
            Assert.True(registry.Verify("TEST-MIB", "testEntity6", new Integer32(1)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity6", new Integer32(2)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity6", new Integer32(3)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity6", new Integer32(4)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity6", new Integer32(5)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity6", new Integer32(0)));

            // Test TAddress
            Assert.False(registry.Verify("TEST-MIB", "testEntity7", new OctetString(new byte[0])));
            Assert.True(registry.Verify("TEST-MIB", "testEntity7", new OctetString(new byte[] { 0x9 })));
            Assert.False(registry.Verify("TEST-MIB", "testEntity7", new OctetString(longStr.ToString())));
#if !TRIAL
            // SAP type
            Assert.True(registry.Verify("TEST-MIB", "testEntity8", new Integer32(0)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity8", new Integer32(254)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity8", new Integer32(255)));
#endif
            // CountryCode
            Assert.True(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[0])));
            Assert.False(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[] { 0x9 })));
            Assert.True(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[] { 0x9, 0x9 })));
            Assert.False(registry.Verify("TEST-MIB", "testEntity9", new OctetString(longStr.ToString())));
#if !TRIAL
            // CountryCodeITU
            Assert.True(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(0)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(255)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(256)));
#endif
            // CiscoRowOperStatus
            Assert.True(registry.Verify("TEST-MIB", "testEntity11", new Integer32(1)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity11", new Integer32(2)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity11", new Integer32(3)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity11", new Integer32(4)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity11", new Integer32(0)));
#if !TRIAL
            // CiscoPort
            Assert.True(registry.Verify("TEST-MIB", "testEntity12", new Integer32(0)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity12", new Integer32(65535)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity12", new Integer32(65536)));
#endif
            // Custom
            Assert.False(registry.Verify("TEST-MIB", "testEntity13", new Integer32(0)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity13", new Integer32(30000000)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity13", new Integer32(31010000)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity13", new Integer32(13750000)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity13", new Integer32(14510000)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity13", new Integer32(5850000)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity13", new Integer32(6425100)));
            Assert.True(registry.Verify("TEST-MIB", "testEntity13", new Integer32(7900000)));
            Assert.False(registry.Verify("TEST-MIB", "testEntity13", new Integer32(8401000)));
        }

        [Fact]
        public void TestChoice()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(new MemoryStream(Resources.RFC1155_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.Test1), collector));
            registry.Refresh();

            // Test CHOICE
#if !TRIAL
            {
                var choiceValue = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity14").DisplayEntity;
                var inner = choiceValue.ResolvedSyntax.GetLastType();
                var inType = inner as ChoiceType;
                Assert.NotNull(inType);
                Assert.Equal(1, inType.ElementTypes.Count);
                var item1 = inType.ElementTypes[0] as TaggedElementType;
                Assert.Equal("internet", item1.Name);
                var root = item1.Subtype.GetLastType();
                Assert.Equal("Lextm.SharpSnmpPro.Mib.IpAddressType", root.ToString());
            }
#endif
        }

        // ReSharper disable InconsistentNaming
        [Fact]
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
            Assert.True(registry.ValidateTable(table));
            Assert.False(registry.ValidateTable(entry));
            Assert.False(registry.ValidateTable(unknown));
        }

        [Fact]
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
            Assert.Equal("::iso", result);
        }
        [Fact]
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
            Assert.Equal("SNMPv2-SMI::transmission", result2);
        }

        [Fact]
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
            Assert.Equal("SNMPv2-MIB::system", result);
        }

        [Fact]
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
            Assert.Equal("SNMPv2-TM::snmpUDPDomain", result);
        }

        [Fact]
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
            Assert.Equal(expected, result);
        }

        [Fact]
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
            Assert.Equal(expected, result);
        }

        [Fact]
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
            Assert.Equal(new uint[] { 0 }, registry.Translate("::ccitt"));
            const string textual = "SNMPv2-SMI::zeroDotZero";
            var expected = new uint[] { 0, 0 };
            Assert.Equal(textual, registry.Translate(expected));
            Assert.Equal(expected, registry.Translate(textual));
        }

        [Fact]
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
            Assert.Equal(expected, result);
        }

        [Fact]
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
            Assert.Equal(expected, result);
        }

        [Fact]
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
            Assert.True(registry.ValidateTable(new ObjectIdentifier(id)));
#endif 
            var table = registry.Tree.Find("SNMPv2-MIB", "sysORTable");

            // query entry by name.
            var entry = registry.Tree.Find("SNMPv2-MIB", "sysOREntry");

            // query column by name.
            var column1 = registry.Tree.Find("SNMPv2-MIB", "sysORIndex");
            Assert.Equal(DefinitionType.Table, table.Type);
            Assert.Equal(DefinitionType.Entry, entry.Type);
            Assert.Equal(DefinitionType.Column, column1.Type);

            // query entry from table.
            Assert.Equal(1, table.Children.Count);
            var entryFromTable = table.Children[0];
            Assert.Equal(DefinitionType.Entry, entryFromTable.Type);
            Assert.Equal("SNMPv2-MIB::sysOREntry", entryFromTable.DisplayName);

            // query column from entry.
            Assert.Equal(4, entry.Children.Count);
            var column2FromEntry = entry.Children[1];
            Assert.Equal(DefinitionType.Column, column2FromEntry.Type);
            Assert.Equal("SNMPv2-MIB::sysORID", column2FromEntry.DisplayName);
        }

        [Fact]
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
            Assert.Equal(expected, id);
        }

        [Fact]
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
            Assert.Equal(expected, value);
        }

        [Fact]
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
            Assert.False(registry.ValidateTable(new ObjectIdentifier(id)));
#endif
        }

        [Fact]
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

            Assert.Equal(new uint[] { 1, 3, 6, 1, 4, 1, 17471 }, id);
            Assert.Equal("ACTONA-ACTASTOR-MIB::actona", registry.Translate(id));
        }

        [Fact]
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

            Assert.Equal("IEEE802dot11-MIB::dot11SMTnotification", registry.Translate(new uint[] { 1, 2, 840, 10036, 1, 6 }));
            uint[] id = registry.Translate("IEEE802dot11-MIB::dot11SMTnotification");
            Assert.Equal(new uint[] { 1, 2, 840, 10036, 1, 6 }, id);

            const string name1 = "IEEE802dot11-MIB::dot11Disassociate";
            var id1 = new uint[] { 1, 2, 840, 10036, 1, 6, 0, 1 };
            Assert.Equal(id1, registry.Translate(name1));
            Assert.Equal(name1, registry.Translate(id1));
        }

        [Fact]
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

            var o = registry.Tree.Search(ObjectIdentifier.Convert("3.6.1.2.1.25"));
            Assert.Null(o.Definition);
            Assert.Equal(".3.6.1.2.1.25", o.AlternativeText);
            Assert.Equal(".3.6.1.2.1.25", o.Text);
            Assert.Equal(1, collector.Errors.Count);
        }

        [Fact]
        public void TestHOSTRESOURCES_MIB()
        {
            var registry = new SimpleObjectRegistry();
            var collector = new ErrorRegistry();
            registry.Tree.Collector = collector;
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_SMI), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_CONF), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_TC), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.SNMPv2_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.IANAifType_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.IF_MIB), collector));
            registry.Import(Parser.Compile(new MemoryStream(Resources.HOST_RESOURCES_MIB), collector));
            registry.Refresh();

            Assert.Equal(0, collector.Errors.Count);

            var module = registry.Tree.LoadedModules.FirstOrDefault(mod => mod.Name == "HOST-RESOURCES-MIB");
            Assert.Equal(83, module.Objects.Count);
        }
        // ReSharper restore InconsistentNaming
    }
}
#pragma warning restore 1591