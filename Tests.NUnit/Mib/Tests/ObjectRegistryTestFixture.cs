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
using System.Linq;
using System.Reflection;

#pragma warning disable 1591
namespace Lextm.SharpSnmpPro.Mib.Tests
{
    using Registry;
    using Validation;
    using Parser = Registry.Parser2;

    [TestFixture]
    public class ObjectRegistryTestFixture
    {
        private static string GetLocation(string file)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", file);
        }

        /// <summary>
        /// A test case for DisplayString derived syntax type.
        /// </summary>
        /// <remarks>
        /// DisplayString is a textual convention defined upon OCTET STRING. It has a size constraint from 0 to 255.
        /// 
        /// DisplayString ::= TEXTUAL-CONVENTION
        ///     DISPLAY-HINT "255a"
        ///     STATUS       current
        ///     DESCRIPTION
        ///             "Represents textual information taken from the NVT ASCII
        /// 
        ///             character set, as defined in pages 4, 10-11 of RFC 854.
        /// 
        ///             To summarize RFC 854, the NVT ASCII repertoire specifies:
        /// 
        ///               - the use of character codes 0-127 (decimal)
        /// 
        ///               - the graphics characters (32-126) are interpreted as
        ///                 US ASCII
        /// 
        ///               - NUL, LF, CR, BEL, BS, HT, VT and FF have the special
        ///                 meanings specified in RFC 854
        /// 
        ///               - the other 25 codes have no standard interpretation
        /// 
        ///               - the sequence 'CR LF' means newline
        /// 
        ///               - the sequence 'CR NUL' means carriage-return
        /// 
        ///               - an 'LF' not preceded by a 'CR' means moving to the
        ///                 same column on the next line.
        /// 
        ///               - the sequence 'CR x' for any x other than LF or NUL is
        ///                 illegal.  (Note that this also means that a string may
        ///                 end with either 'CR LF' or 'CR NUL', but not with CR.)
        /// 
        ///             Any object defined using this syntax may not exceed 255
        ///             characters in length."
        ///     SYNTAX       OCTET STRING (SIZE (0..255))
        ///
        /// sysDescr is an object of DisplayString type.
        ///
        /// sysDescr OBJECT-TYPE
        ///     SYNTAX      DisplayString (SIZE (0..255))
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { system 1 }
        /// </remarks>
        [Test]
        public void TestDisplayString()
        {
            var registry = LoadTestingDocuments();

            var item = registry.Tree.Find("SNMPv2-MIB", "sysDescr");
            var entity = item.DisplayEntity;

            // IMPORTANT: basic information of sysDescr is tested.
            Assert.AreEqual("A textual description of the entity.  This value should include the full name and version identification of the system's hardware type, software operating-system, and networking software.", entity.DescriptionFormatted());
            Assert.AreEqual(EntityStatus.Current, entity.Status);
            Assert.AreEqual(string.Empty, entity.Reference);

            var obj = entity as IObjectTypeMacro;
            Assert.AreEqual(Access.ReadOnly, obj.MibAccess);
#if TRIAL
            // IMPORTANT: the Trial edition can only show the base syntax.
            Assert.AreEqual(SnmpType.OctetString, obj.BaseSyntax);
#endif
#if !TRIAL
            Assert.IsTrue(obj.Syntax is ConstraintedType);

            // IMPORTANT: type resolution shows that OCTET STRING is the base syntax type of DisplayString.
            var constrainted = obj.ResolvedSyntax as ConstraintedType; // Syntax = (DisplayString) + (SIZE (0..255))
            Assert.IsNotNull(constrainted);
            Assert.IsNotNull(constrainted.Constraint);

            {
                var specs = constrainted.Constraint.ElementSetSpecs;
                var size = specs.LeftElement.Element as SizeConstraintElement;
                Assert.IsNotNull(size);
                var range = size.Constraint.ElementSetSpecs.LeftElement.Element as ValueRangeConstraintElement;
                Assert.IsNotNull(range);
                Assert.AreEqual("0", range.ValueRange.MinValue.ToString());
                Assert.AreEqual("255", range.ValueRange.MaxValue.ToString());
            }

            var assignment = constrainted.BaseType as TypeAssignment;
            Assert.IsNotNull(assignment);

            var textual = assignment.BaseType as TextualConventionMacro; // DisplayString = (OCTET STRING) + (SIZE (0..255))
            Assert.IsNotNull(textual);

            var constrainted2 = textual.BaseType as ConstraintedType;
            Assert.IsNotNull(constrainted2);
            Assert.IsNotNull(constrainted2.Constraint);

            {
                var specs = constrainted2.Constraint.ElementSetSpecs;
                var size = specs.LeftElement.Element as SizeConstraintElement;
                Assert.IsNotNull(size);
                var range = size.Constraint.ElementSetSpecs.LeftElement.Element as ValueRangeConstraintElement;
                Assert.IsNotNull(range);
                Assert.AreEqual("0", range.ValueRange.MinValue.ToString());
                Assert.AreEqual("255", range.ValueRange.MaxValue.ToString());
            }

            var octet = constrainted2.BaseType as OctetStringType; // OCTET STRING pure type has no constraint.
            Assert.IsNotNull(octet);

            var type = obj.ResolvedSyntax.GetLastType();
            Assert.IsTrue(type is OctetStringType);

            // IMPORTANT: print out DisplayString syntax as string.
            var name = new StringBuilder();
            obj.ResolvedSyntax.Append(name);
            Assert.AreEqual("DisplayString ::= TEXTUAL-CONVENTION\r\nDISPLAY-HINT \"255a\"\r\nSTATUS current\r\nSYNTAX OCTET STRING", name.ToString());

            // IMPORTANT: below we test input data against the SIZE constraint of DisplayString.
            Assert.IsTrue(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString("test")));
            Assert.IsTrue(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString(string.Empty)));
            Assert.IsFalse(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString(Get257Chars())));
#endif
        }

        private static string Get257Chars()
        {
            var longStr = new StringBuilder();
            for (int i = 0; i <= 256; i++)
            {
                longStr.Append('t');
            }

            return longStr.ToString();
        }

        /// <summary>
        /// A test case for INTEGER.
        /// </summary>
        /// <remarks>
        /// ifAdminStatus is an object of INTEGER type.
        /// 
        /// ifAdminStatus OBJECT-TYPE
        ///     SYNTAX  INTEGER {
        ///                 up(1),       -- ready to pass packets
        ///                 down(2),
        ///                 testing(3)   -- in some test mode
        ///             }
        ///     MAX-ACCESS  read-write
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "The desired state of the interface.  The testing(3) state
        ///             indicates that no operational packets can be passed.  When a
        ///             managed system initializes, all interfaces start with
        ///             ifAdminStatus in the down(2) state.  As a result of either
        ///             explicit management action or per configuration information
        ///             retained by the managed system, ifAdminStatus is then
        ///             changed to either the up(1) or testing(3) states (or remains
        ///             in the down(2) state)."
        ///     ::= { ifEntry 7 }*/
        /// </remarks>
        [Test]
        public void TestInteger()
        {
            var registry = LoadTestingDocuments();

            // IMPORTANT: test input data against the syntax.
            Assert.IsTrue(registry.Verify("IF-MIB", "ifAdminStatus", new Integer32(2)));
            Assert.IsFalse(registry.Verify("IF-MIB", "ifAdminStatus", new Integer32(5)));

            // IMPORTANT: decode the input data to a suitable format.
            Assert.AreEqual("down(2)", registry.Decode("IF-MIB", "ifAdminStatus", new Integer32(2)));
        }

        /// <summary>
        /// A test case for BITS.
        /// </summary>
        /// <remarks>
        /// CiscoCosList is a textual convention upon BITS, and testEntity is an object of this type.
        /// 
        /// CiscoCosList ::= TEXTUAL-CONVENTION
        ///     STATUS          current
        ///     DESCRIPTION
        ///         "Each bit represents a CoS value (0 through 7)."
        ///     SYNTAX          BITS {
        ///                         cos0(0),
        ///                         cos1(1),
        ///                         cos2(2),
        ///                         cos3(3),
        ///                         cos4(4),
        ///                         cos5(5),
        ///                         cos6(6),
        ///                         cos7(7)
        ///                     }
        /// 
        /// testEntity OBJECT-TYPE
        ///     SYNTAX      CiscoCosList
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 1 }
        /// </remarks>
        [Test]
        public void TestBits()
        {
            var registry = LoadTestingDocuments();
#if !TRIAL
            // Test BITS
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity", new OctetString(new byte[] { 0x8 })));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity", new OctetString(new byte[] { 0x8, 0x9 })));
            var bits = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity").DisplayEntity;

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

            // TODO: how to decode BITS?
            // Assert.AreEqual("down(2)", registry.Decode("TEST-MIB", "testEntity", new OctetString(new byte[] { 0x8 })));
#endif
        }

        /// <summary>
        /// A test case for TruthValue.
        /// </summary>
        /// <remarks>
        /// TruthValue is a textual convention upon INTEGER, and testEntity2 is an object of this type.
        /// 
        /// TruthValue ::= TEXTUAL-CONVENTION
        ///     STATUS       current
        ///     DESCRIPTION
        ///             "Represents a boolean value."
        ///     SYNTAX       INTEGER { true(1), false(2) }
        /// 
        /// testEntity2 OBJECT-TYPE
        ///     SYNTAX      TruthValue
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 2 }
        /// </remarks>
        [Test]
        public void TestTruthValue()
        {
            var registry = LoadTestingDocuments();

            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity2", new Integer32(1)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity2", new Integer32(2)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity2", new Integer32(0)));
#if !TRIAL
            var entityTruthValue = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity2").DisplayEntity;
            var truthValueName = new StringBuilder();
            entityTruthValue.ResolvedSyntax.Append(truthValueName);
            Assert.AreEqual("TruthValue ::= TEXTUAL-CONVENTION\r\nSTATUS current\r\nSYNTAX INTEGER { true(1), false(2) }", truthValueName.ToString());

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
#endif

            Assert.AreEqual("true(1)", registry.Decode("TEST-MIB", "testEntity2", new Integer32(1)));
            Assert.AreEqual("false(2)", registry.Decode("TEST-MIB", "testEntity2", new Integer32(2)));
        }

        /// <summary>
        /// A test case for MacAddress.
        /// </summary>
        /// <remarks>
        /// MacAddress is a textual convention upon OCTET STRING, and testEntity3 is an object of this syntax.
        /// 
        /// MacAddress ::= TEXTUAL-CONVENTION
        ///     DISPLAY-HINT "1x:"
        ///     STATUS       current
        ///     DESCRIPTION
        ///             "Represents an 802 MAC address represented in the
        ///             `canonical' order defined by IEEE 802.1a, i.e., as if it
        ///             were transmitted least significant bit first, even though
        ///             802.5 (in contrast to other 802.x protocols) requires MAC
        ///             addresses to be transmitted most significant bit first."
        ///     SYNTAX       OCTET STRING (SIZE (6))
        /// 
        /// testEntity3 OBJECT-TYPE
        ///     SYNTAX      MacAddress
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 3 }
        /// </remarks>
        [Test]
        public void TestMacAddress()
        {
            var registry = LoadTestingDocuments();

            // Test MacAddress
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9 })));

            Assert.AreEqual("09-09-09-09-09-10", registry.Decode("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x10 })));
        }

        /// <summary>
        /// A test case for RowStatus.
        /// </summary>
        /// <remarks>
        /// RowStatus is a textual convention upon INTEGER.
        /// 
        /// RowStatus ::= TEXTUAL-CONVENTION
        ///     STATUS       current
        ///     DESCRIPTION
        ///             "The RowStatus textual convention is used to manage the
        ///             creation and deletion of conceptual rows, and is used as the
        ///             value of the SYNTAX clause for the status column of a
        ///             conceptual row (as described in Section 7.7.1 of [2].)"
        /// 
        ///     SYNTAX       INTEGER {
        ///             -- the following two values are states:
        ///             -- these values may be read or written
        ///             active(1),
        ///             notInService(2),
        ///             -- the following value is a state:
        ///             -- this value may be read, but not written
        ///             notReady(3),
        ///             -- the following three values are
        ///             -- actions: these values may be written,
        ///             --   but are never read
        ///             createAndGo(4),
        ///             createAndWait(5),
        ///             destroy(6)
        ///         }
        /// 
        /// testEntity4 OBJECT-TYPE
        ///     SYNTAX      RowStatus
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 4 }
        /// </remarks>
        [Test]
        public void TestRowStatus()
        {
            var registry = LoadTestingDocuments();

            // Test RowStatus
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(1)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(2)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(3)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(4)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(5)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity4", new Integer32(6)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity4", new Integer32(0)));
        }

        /// <summary>
        /// A test case for DateAndTime.
        /// </summary>
        /// <remarks>
        /// DateAndTime is a textual convention upon OCTET STRING.
        /// 
        /// DateAndTime ::= TEXTUAL-CONVENTION
        ///     DISPLAY-HINT "2d-1d-1d,1d:1d:1d.1d,1a1d:1d"
        ///     STATUS       current
        ///     DESCRIPTION
        ///             "A date-time specification.
        /// 
        ///             field  octets  contents                  range
        ///             -----  ------  --------                  -----
        ///               1      1-2   year*                     0..65536
        ///               2       3    month                     1..12
        ///               3       4    day                       1..31
        ///               4       5    hour                      0..23
        ///               5       6    minutes                   0..59
        ///               6       7    seconds                   0..60
        ///                            (use 60 for leap-second)
        ///               7       8    deci-seconds              0..9
        ///               8       9    direction from UTC        '+' / '-'
        ///               9      10    hours from UTC*           0..13
        ///              10      11    minutes from UTC          0..59
        /// 
        ///             * Notes:
        ///             - the value of year is in network-byte order
        ///             - daylight saving time in New Zealand is +13
        /// 
        ///             For example, Tuesday May 26, 1992 at 1:30:15 PM EDT would be
        ///             displayed as:
        /// 
        ///                              1992-5-26,13:30:15.0,-4:0
        /// 
        ///             Note that if only local time is known, then timezone
        ///             information (fields 8-10) is not present."
        ///     SYNTAX       OCTET STRING (SIZE (8 | 11))
        /// 
        /// testEntity5 OBJECT-TYPE
        ///     SYNTAX      DateAndTime
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 5 }
        /// </remarks>
        [Test]
        public void TestDateAndTime()
        {
            var registry = LoadTestingDocuments();

            // DateAndTime
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.Throws<InvalidOperationException>(() => registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9 })));

            Assert.AreEqual("2004-08-17T15:48:00.0000000-05:00", registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xD4, 0x08, 0x11, 0x0F, 0x30, 0x00, 0x00, 0x2D, 0x05, 0x00 })));
            Assert.AreEqual("2004-08-17T15:48:00.0000000+00:00", registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xD4, 0x08, 0x11, 0x0F, 0x30, 0x00, 0x00 })));
            Assert.AreEqual("1992-05-26T13:30:15.0000000-04:00", registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xC8, 5, 26, 13, 30, 15, 0x00, 0x2D, 0x04, 0x00 })));
        }

        /// <summary>
        /// A test case for StorageType.
        /// </summary>
        /// <remarks>
        /// The StorageType is a textual convention upon INTEGER.
        /// 
        /// StorageType ::= TEXTUAL-CONVENTION
        ///     STATUS       current
        ///     DESCRIPTION
        ///             "Describes the memory realization of a conceptual row.  A
        ///             row which is volatile(2) is lost upon reboot.  A row which
        ///             is either nonVolatile(3), permanent(4) or readOnly(5), is
        ///             backed up by stable storage.  A row which is permanent(4)
        ///             can be changed but not deleted.  A row which is readOnly(5)
        ///             cannot be changed nor deleted.
        /// 
        ///             If the value of an object with this syntax is either
        ///             permanent(4) or readOnly(5), it cannot be written.
        ///             Conversely, if the value is either other(1), volatile(2) or
        ///             nonVolatile(3), it cannot be modified to be permanent(4) or
        ///             readOnly(5).  (All illegal modifications result in a
        ///             'wrongValue' error.)
        /// 
        ///             Every usage of this textual convention is required to
        ///             specify the columnar objects which a permanent(4) row must
        ///             at a minimum allow to be writable."
        ///     SYNTAX       INTEGER {
        ///                      other(1),       -- eh?
        ///                      volatile(2),    -- e.g., in RAM
        ///                      nonVolatile(3), -- e.g., in NVRAM
        ///                      permanent(4),   -- e.g., partially in ROM
        ///                      readOnly(5)     -- e.g., completely in ROM
        ///                  }
        /// 
        /// testEntity6 OBJECT-TYPE
        ///     SYNTAX      StorageType
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 6 }
        /// </remarks>
        [Test]
        public void TestStorageType()
        {
            var registry = LoadTestingDocuments();

            // StorageType
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity6", new Integer32(1)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity6", new Integer32(2)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity6", new Integer32(3)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity6", new Integer32(4)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity6", new Integer32(5)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity6", new Integer32(0)));
        }

        /// <summary>
        /// A test case for TAddress.
        /// </summary>
        /// <remarks>
        /// TAddress is a textual convention upon OCTET STRING.
        /// 
        /// TAddress ::= TEXTUAL-CONVENTION
        ///     STATUS       current
        ///     DESCRIPTION
        ///           "Denotes a transport service address.
        /// 
        ///           A TAddress value is always interpreted within the context of a
        ///           TDomain value.  Thus, each definition of a TDomain value must
        ///           be accompanied by a definition of a textual convention for use
        ///           with that TDomain.  Some possible textual conventions, such as
        ///           SnmpUDPAddress for snmpUDPDomain, are defined in the SNMPv2-TM
        ///           MIB module.  Other possible textual conventions are defined in
        ///           other MIB modules."
        ///     REFERENCE    "The SNMPv2-TM MIB module is defined in RFC 1906."
        ///     SYNTAX       OCTET STRING (SIZE (1..255))
        /// 
        /// testEntity7 OBJECT-TYPE
        ///     SYNTAX      TAddress
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 7 }
        /// </remarks>
        [Test]
        public void TestTAddress()
        {
            var registry = LoadTestingDocuments();

            // Test TAddress
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity7", new OctetString(new byte[0])));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity7", new OctetString(new byte[] { 0x9 })));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity7", new OctetString(Get257Chars())));
        }

        /// <summary>
        /// A test case for SAP.
        /// </summary>
        /// <remarks>
        /// SAPType ::= TEXTUAL-CONVENTION
        ///     STATUS          current
        ///     DESCRIPTION
        ///         "Service Access Point - is a term that denotes the means
        ///         by which a user entity in layer n+1 accesses a service
        ///         of a provider entity in layer n."
        ///     SYNTAX          Integer32 (0..254)
        /// 
        /// testEntity8 OBJECT-TYPE
        ///     SYNTAX      SAPType
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 8 }
        /// </remarks>
        [Test]
        public void TestSAP()
        {
            var registry = LoadTestingDocuments();
#if !TRIAL
            // SAP type
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity8", new Integer32(0)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity8", new Integer32(254)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity8", new Integer32(255)));
#endif
        }

        /// <summary>
        /// A test case for CountryCode.
        /// </summary>
        /// <remarks>
        /// CountryCode ::= TEXTUAL-CONVENTION
        ///     STATUS          current
        ///     DESCRIPTION
        ///         "Represents a case-insensitive 2-letter country code taken
        ///         from ISO-3166. Unrecognized countries are represented as 
        ///         empty string."
        ///     SYNTAX          OCTET STRING (SIZE (0 | 2))
        /// 
        /// testEntity9 OBJECT-TYPE
        ///     SYNTAX      CountryCode
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 9 }
        /// </remarks>
        [Test]
        public void TestCountryCode()
        {
            var registry = LoadTestingDocuments();

            // CountryCode
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[0])));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[] { 0x9 })));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[] { 0x9, 0x9 })));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity9", new OctetString(Get257Chars())));
        }

        /// <summary>
        /// A test case for CountryCodeITU.
        /// </summary>
        /// <remarks>
        /// testEntity10 OBJECT-TYPE
        ///     SYNTAX      CountryCodeITU
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 10 }
        /// 
        /// CountryCodeITU ::= TEXTUAL-CONVENTION
        ///     STATUS          current
        ///     DESCRIPTION
        ///         "This textual convention represents a country or area code for
        ///         non-standard facilities in telematic services."
        /// 
        ///     REFERENCE       "ITU-T T.35 - Section 3.1 Country Code"
        ///     SYNTAX          Unsigned32 (0..255)
        /// </remarks>
        [Test]
        public void TestCountryCodeITU()
        {
            var registry = LoadTestingDocuments();
#if !TRIAL
            // CountryCodeITU
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(0)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(255)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(256)));
#endif
        }

        /// <summary>
        /// A test case for CiscoRowOperStatus.
        /// </summary>
        /// <remarks>
        /// CiscoRowOperStatus ::= TEXTUAL-CONVENTION
        ///     STATUS          current
        ///     DESCRIPTION
        ///         "Represents the operational status of an table entry.
        ///         This textual convention allows explicitly representing
        ///         the states of rows dependent on rows in other tables.
        /// 
        ///         active(1) -
        ///             Indicates this entry's RowStatus is active
        ///             and the RowStatus for each dependency is active.
        /// 
        ///         activeDependencies(2) -
        ///             Indicates that the RowStatus for each dependency
        ///             is active, but the entry's RowStatus is not active.
        /// 
        ///         inactiveDependency(3) -
        ///             Indicates that the RowStatus for at least one
        ///             dependency is not active.
        /// 
        ///         missingDependency(4) -
        ///             Indicates that at least one dependency does
        ///             not exist in it's table."
        ///     SYNTAX          INTEGER  {
        ///                         active(1),
        ///                         activeDependencies(2),
        ///                         inactiveDependency(3),
        ///                         missingDependency(4)
        ///                     }
        /// 
        /// testEntity11 OBJECT-TYPE
        ///     SYNTAX      CiscoRowOperStatus
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 11 }
        /// </remarks>
        [Test]
        public void TestCiscoRowOperStatus()
        {
            var registry = LoadTestingDocuments();

            // CiscoRowOperStatus
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity11", new Integer32(1)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity11", new Integer32(2)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity11", new Integer32(3)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity11", new Integer32(4)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity11", new Integer32(0)));
        }

        /// <summary>
        /// A test case for CiscoPort.
        /// </summary>
        /// <remarks>
        /// CiscoPort ::= TEXTUAL-CONVENTION
        ///     STATUS          current
        ///     DESCRIPTION
        ///         "The TCP or UDP port number range."
        /// 
        ///     REFERENCE
        ///         "Transmission Control Protocol. J. Postel. RFC793,
        ///             User Datagram Protocol. J. Postel. RFC768"
        ///     SYNTAX          Integer32 (0..65535)
        /// 
        /// testEntity12 OBJECT-TYPE
        ///     SYNTAX      CiscoPort
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 12 }
        /// </remarks>
        [Test]
        public void TestCiscoPort()
        {
            var registry = LoadTestingDocuments();
#if !TRIAL
            // CiscoPort
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity12", new Integer32(0)));
            Assert.IsTrue(registry.Verify("TEST-MIB", "testEntity12", new Integer32(65535)));
            Assert.IsFalse(registry.Verify("TEST-MIB", "testEntity12", new Integer32(65536)));
#endif
        }

        /// <summary>
        /// A test case for custom type.
        /// </summary>
        /// <remarks>
        /// testEntity13 OBJECT-TYPE
        ///     SYNTAX      INTEGER (30000000..31000000 | 13750000..14500000 | 5850000..6425000 | 7900000..8400000)
        ///     MAX-ACCESS  read-only
        ///     STATUS      current
        ///     DESCRIPTION
        ///             "A textual description of the entity.  This value should
        ///             include the full name and version identification of
        ///             the system's hardware type, software operating-system,
        ///             and networking software."
        ///     ::= { test 13 }        
        /// </remarks>
        [Test]
        public void TestCustom()
        {
            var registry = LoadTestingDocuments();

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

        private static ObjectRegistryBase LoadTestingDocuments()
        {
            var collector = new ErrorRegistry();
            return new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("Test.mib"), collector))
                .Import(Parser.Compile(GetLocation("CISCO-SMI.mib"), collector))
                .Import(Parser.Compile(GetLocation("CISCO-TC.mib"), collector))
                .Refresh();
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
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("Test1.mib"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
            Assert.AreEqual(2, collector.Warnings.Count);

#if !TRIAL
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
#endif
        }

        // ReSharper disable InconsistentNaming
        [Test]
        public void TestValidateTable()
        {
            var table = new ObjectIdentifier(new uint[] { 1, 3, 6, 1, 2, 1, 1, 9 });
            var entry = new ObjectIdentifier(new uint[] { 1, 3, 6, 1, 2, 1, 1, 9, 1 });
            var unknown = new ObjectIdentifier(new uint[] { 1, 3, 6, 8, 18579, 111111 });
            
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Refresh();
            Assert.IsTrue(registry.ValidateTable(table));
            Assert.IsFalse(registry.ValidateTable(entry));
            Assert.IsFalse(registry.ValidateTable(unknown));
        }

        [Test]
        public void TestGetTextualForms()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
            Assert.AreEqual(0, collector.Warnings.Count);
            const string iso = "::iso";
            Assert.AreEqual(iso, registry.Translate(new uint[] { 1 }));
            Assert.AreEqual(new uint[] { 1 }, registry.Translate(iso));
            const string transmission = "SNMPv2-SMI::transmission";
            Assert.AreEqual(transmission, registry.Translate(new uint[] { 1, 3, 6, 1, 2, 1, 10 }));
            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 2, 1, 10 }, registry.Translate(transmission));

            Assert.AreEqual("SNMPv2-MIB::system", registry.Translate(new uint[] { 1, 3, 6, 1, 2, 1, 1 }));
            const string domain = "SNMPv2-TM::snmpUDPDomain";
            Assert.AreEqual(domain, registry.Translate(ObjectIdentifier.AppendTo(registry.Translate("SNMPv2-SMI::snmpDomains"), 1)));
            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 6, 1, 1 }, registry.Translate(domain));

            Assert.AreEqual(new uint[] { 0 }, registry.Translate("::ccitt"));
            const string zero = "SNMPv2-SMI::zeroDotZero";
            Assert.AreEqual(zero, registry.Translate(new uint[] { 0, 0 }));
            Assert.AreEqual(new uint[] { 0, 0 }, registry.Translate(zero));

            var item = registry.Tree.Find("SNMPv2-SMI", "zeroDotZero");
            Assert.AreEqual(new uint[] { 0, 0 }, item.DisplayEntity.GetObjectIdentifier());

            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 2, 1, 1 }, registry.Translate("SNMPv2-MIB::system"));
        }

        [Test]
        public void TestsysORTable()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
            Assert.AreEqual(0, collector.Warnings.Count);

            uint[] id = registry.Translate("SNMPv2-MIB::sysORTable");
#if !TRIAL
            Assert.IsTrue(registry.ValidateTable(new ObjectIdentifier(id)));
#endif
            var node = registry.Tree.Find("SNMPv2-MIB", "sysORTable");
            var node1 = registry.Tree.Find("SNMPv2-MIB", "sysOREntry");
            var node2 = registry.Tree.Find("SNMPv2-MIB", "sysORIndex");
            Assert.AreEqual(DefinitionType.Table, node.Type);
            Assert.AreEqual(DefinitionType.Entry, node1.Type);
            Assert.AreEqual(DefinitionType.Column, node2.Type);

            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 2, 1, 1, 9, 0 }, registry.Translate("SNMPv2-MIB::sysORTable.0"));
            Assert.AreEqual("SNMPv2-MIB::sysORTable.0", registry.Translate(new uint[] { 1, 3, 6, 1, 2, 1, 1, 9, 0 }));

#if !TRIAL
            Assert.IsFalse(registry.ValidateTable(new ObjectIdentifier(registry.Translate("SNMPv2-MIB::snmpMIB"))));
#endif
        }

        [Test]
        public void TestActona()
        {
            const string name = "ACTONA-ACTASTOR-MIB::actona";
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(GetLocation("ACTONA-ACTASTOR-MIB.mib"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
            Assert.AreEqual(0, collector.Warnings.Count);

            uint[] id = registry.Translate(name);

            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 4, 1, 17471 }, id);
            Assert.AreEqual(name, registry.Translate(id));
        }

        [Test]
        public void TestIEEE802dot11_MIB()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("RFC-1212"), collector))
                .Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("RFC1213-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(GetLocation("IEEE802DOT11-MIB.mib"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
#if !TRIAL
            Assert.AreEqual(3, collector.Warnings.Count);
#endif
            const string notification = "IEEE802dot11-MIB::dot11SMTnotification";
            Assert.AreEqual(notification, registry.Translate(new uint[] { 1, 2, 840, 10036, 1, 6 }));
            uint[] id = registry.Translate(notification);
            Assert.AreEqual(new uint[] { 1, 2, 840, 10036, 1, 6 }, id);

            const string name1 = "IEEE802dot11-MIB::dot11Disassociate";
            var id1 = new uint[] { 1, 2, 840, 10036, 1, 6, 0, 1 };
            Assert.AreEqual(id1, registry.Translate(name1));
            Assert.AreEqual(name1, registry.Translate(id1));
        }

        [Test]
        public void TestJVM_MANAGEMENT_MIB()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("RFC-1212"), collector))
                .Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("RFC1213-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(GetLocation("JVM-MANAGEMENT-MIB.mib"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
#if !TRIAL
            Assert.AreEqual(4, collector.Warnings.Count);
#endif
            const string jmgt = "JVM-MANAGEMENT-MIB::jmgt";
            Assert.AreEqual(jmgt, registry.Translate(new uint[] { 1, 3, 6, 1, 4, 1, 42, 2, 145 }));
            uint[] id = registry.Translate(jmgt);
            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 4, 1, 42, 2, 145 }, id);

            var item = registry.Tree.Find("JVM-MANAGEMENT-MIB", "jmgt");
            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 4, 1, 42, 2, 145 }, item.DisplayEntity.GetObjectIdentifier());
        }


        [Test]
        public void TestALLIEDTELESYN_MIB()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("RFC-1212"), collector))
                .Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("RFC1213-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(GetLocation("ALLIEDTELESYN-MIB.mib"), collector))
                .Refresh();
#if !TRIAL
            Assert.AreEqual(2, collector.Warnings.Count);
#endif
            var o = registry.Tree.Search(ObjectIdentifier.Convert("3.6.1.2.1.25"));
            Assert.IsNull(o.Definition);
            Assert.AreEqual(".3.6.1.2.1.25", o.AlternativeText);
            Assert.AreEqual(".3.6.1.2.1.25", o.Text);
            Assert.AreEqual(1, collector.Errors.Count);
        }

        [Test]
        public void TestHOSTRESOURCES_MIB()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("HOST-RESOURCES-MIB.txt"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
#if !TRIAL
            Assert.AreEqual(0, collector.Warnings.Count);
#endif
            var module = registry.Tree.LoadedModules.First(mod => mod.Name == "HOST-RESOURCES-MIB");
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
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IEEE8021-TC-MIB.txt"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
            Assert.AreEqual(14, collector.Warnings.Count);

            var zero = registry.Translate(new uint[] { 0, 0 });
            Assert.AreEqual("SNMPv2-SMI::zeroDotZero", zero);

            var module = registry.Tree.LoadedModules.FirstOrDefault(mod => mod.Name == "IEEE8021-TC-MIB");
            Assert.AreEqual(0, module.Objects.Count);
            var child = registry.Translate("IEEE8021-TC-MIB::ieee8021TcMib");
            Assert.AreEqual("1.3.111.2.802.1.1.1", ObjectIdentifier.Convert(child));
            var parent = registry.Translate("IEEE8021-TC-MIB::ieee802dot1mibs");
            Assert.AreEqual("1.3.111.2.802.1.1", ObjectIdentifier.Convert(parent));

            // IMPORTANT: assistant OIDs were utilized in 1.1.1 and older releases to support such scenarios. They are no longer required in 1.1.2 and above.
            Assert.IsNull(registry.Tree.Find("IEEE8021-TC-MIB", "ieee802dot1_1"));

            var definition = registry.Tree.Find("IEEE8021-TC-MIB", "ieee802dot1mibs");
            // IMPORTANT: since no more assistant OIDs exists, the textual form is now unique.
            Assert.AreEqual(1, definition.TextualForms.Count);
        }

        [Test]
        public void TestFoundry()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("INET-ADDRESS-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("HCNUM-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("FOUNDRY-SN-ROOT-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("FOUNDRY-SN-AGENT-MIB.txt"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
#if !TRIAL
            Assert.AreEqual(10, collector.Warnings.Count);
#endif
        }

        // ReSharper restore InconsistentNaming

        /// <summary> 
        /// A test case for IMPLIED keyword. 
        /// </summary> 
        /// <remarks> 
        /// The table entry definition is as below, 
        ///  
        /// snmpTargetAddrEntry OBJECT-TYPE 
        ///     SYNTAX SnmpTargetAddrEntry 
        ///     MAX-ACCESS not-accessible 
        ///     STATUS      current 
        ///     DESCRIPTION 
        ///         "A transport address to be used in the generation 
        ///          of SNMP operations. 
        ///  
        ///          Entries in the snmpTargetAddrTable are created and 
        ///          deleted using the snmpTargetAddrRowStatus object." 
        ///     INDEX { IMPLIED snmpTargetAddrName } 
        ///     ::= { snmpTargetAddrTable 1 } 
        /// </remarks> 
        [Test]
        public void TestImplied()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMP-FRAMEWORK-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMP-TARGET-MIB.txt"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
            Assert.AreEqual(0, collector.Warnings.Count);

            var definition = registry.Tree.Find("SNMP-TARGET-MIB", "snmpTargetAddrEntry");
            var type = definition.DisplayEntity as IObjectTypeMacro;
            Assert.IsNotNull(type);
#if !TRIAL
            var real = (ObjectTypeMacro)type;
            Assert.AreEqual(1, real.IndexList.Count);
            var index = real.IndexList[0];
            Assert.AreEqual("snmpTargetAddrName", index.Type.Name);
            Assert.IsTrue(index.Implied);
#endif
        }

        [Test]
        public void TestAugments()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("INET-ADDRESS-MIB.txt"), collector))
                .Refresh();

            var definition = registry.Tree.Find("IF-MIB", "ifXEntry");
            var type = definition.DisplayEntity as IObjectTypeMacro;
            Assert.IsNotNull(type);
#if !TRIAL
            var real = (ObjectTypeMacro)type;
            Assert.IsNotNull(real.Augments);

            Assert.AreEqual("ifEntry", real.Augments.Type.Name);
            Assert.AreEqual("IF-MIB", real.Augments.Type.Module.Name);
            Assert.IsTrue(real.Augments.Type.ResolvedSyntax.GetLastType() is SequenceType);
#endif
        }

        [Test]
        public void TestDuplicateModule()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("empty.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Refresh();

            Assert.AreEqual(4, collector.Errors.Count);
            Assert.AreEqual(0, collector.Warnings.Count);

            var item = collector.Errors.ElementAt(0);
#if !TRIAL
            Assert.AreEqual(ErrorCategory.DuplicateModule, item.Category);
#endif
        }

        [Test]
        public void TestInvalidDocument()
        {
            var file = GetLocation("Invalid.txt");
            var collector = new ErrorRegistry();
            new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(file, collector))
                .Refresh();
            Assert.AreEqual(1, collector.Errors.Count);

            var item = collector.Errors.ElementAt(0);
            Assert.AreEqual(ErrorCategory.SematicError, item.Category);
            Assert.AreEqual($"{file} (1,6) : error S0001 : Invalid token 'is'.", item.ToString());
        }

        [Test]
        public void TestDocsCableDeviceTrapMib()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("INET-ADDRESS-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMP-FRAMEWORK-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("DOCS-IF-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("DOCS-IF-EXT-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("DOCS-CABLE-DEVICE-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("DOCS-CABLE-DEVICE-TRAP-MIB.mib"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
#if !TRIAL
            Assert.AreEqual(2, collector.Warnings.Count);
#endif
            var definition = registry.Tree.Find("DOCS-CABLE-DEVICE-TRAP-MIB", "docsDevCmInitTLVUnknownTrap");
            Assert.AreEqual("DOCS-CABLE-DEVICE-TRAP-MIB::docsDevCmInitTLVUnknownTrap", registry.Translate(definition.GetNumericalForm()));
#if !TRIAL
            var type = definition.DisplayEntity as NotificationTypeMacro;
            Assert.IsNotNull(type);
#endif
        }

        [Test]
        public void TestSonicWallFirewallTrapMibMib()
        {
            var collector = new ErrorRegistry();
            new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SONICWALL-SMI.mib"), collector))
                .Import(Parser.Compile(GetLocation("SONICWALL-FIREWALL-TRAP-MIB.mib"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
            Assert.AreEqual(1, collector.Warnings.Count);
            var warning = collector.Warnings.First();
            Assert.AreEqual(WarningCategory.ImplicitNodeCreation, warning.Category);
        }

        [Test]
        public void TestADSL()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("INET-ADDRESS-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("PERFHIST-TC-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("SNMP-FRAMEWORK-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("ADSL-TC-MIB.mib"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
#if !TRIAL
            Assert.AreEqual(1, collector.Warnings.Count);
#endif
            uint[] id = registry.Translate("ADSL-TC-MIB::adsltcmib");
            Assert.IsNull(id);
            // IMPORTANT: The module is not pending, but its contents cannot be put on to the tree.
            // Assert.AreEqual(new uint[] { 1, 3, 6, 1, 4, 1, 17471 }, id);
            // Assert.AreEqual("ADSL-TC-MIB::adsltcmib", registry.Translate(id));
        }

        [Test]
        public void TestIEEE8023LAG_MIB()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("RFC-1212"), collector))
                .Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("RFC1213-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("RFC1271-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("RFC-1215.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("INET-ADDRESS-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("PERFHIST-TC-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("SNMP-FRAMEWORK-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("RMON-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("TOKEN-RING-RMON-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("RMON2-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("BRIDGE-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("P-BRIDGE-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("Q-BRIDGE-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("IEEE8023-LAG-MIB.mib"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
#if !TRIAL
            Assert.AreEqual(26, collector.Warnings.Count);
#endif
            {
                const string lag = "IEEE8023-LAG-MIB::lagMIB";
                Assert.AreEqual(lag, registry.Translate(new uint[] { 1, 2, 840, 10006, 300, 43 }));
                uint[] id = registry.Translate(lag);
                Assert.AreEqual(new uint[] { 1, 2, 840, 10006, 300, 43 }, id);
            }
            {
                const string dot = "IEEE8023-LAG-MIB::802dot3";
                Assert.AreEqual(dot, registry.Translate(new uint[] { 1, 2, 840, 10006 }));
                uint[] id = registry.Translate(dot);
                Assert.AreEqual(new uint[] { 1, 2, 840, 10006 }, id);
            }
        }

        [Test]
        public void TestCisco()
        {
            var collector = new ErrorRegistry();
            new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("INET-ADDRESS-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("PERFHIST-TC-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("SNMP-FRAMEWORK-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("CISCO-SMI.mib"), collector))
                .Import(Parser.Compile(GetLocation("CISCO-IETF-NAT-MIB.mib"), collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);
#if !TRIAL
            Assert.AreEqual(2, collector.Warnings.Count);
#endif
            foreach (var warning in collector.Warnings)
            {
                Assert.AreEqual(WarningCategory.WrongIndexType, warning.Category);
            }
        }
        
        [Test]
        public void TestInvalidImport()
        {
            // IMPORTANT: SMIv2 forbids such imports.
            var test = GetLocation("TestInvalidImport");
            var builder = new StringBuilder("TEST-MIB DEFINITIONS ::= BEGIN")
                .AppendLine()
                .AppendLine("IMPORTS")
                .AppendLine("    MODULE-IDENTITY,")
                .AppendLine("    BITS,")
                .AppendLine("    INTEGER,")
                .AppendLine("    OCTET STRING,")
                .AppendLine("    OBJECT IDENTIFIER,")
                .AppendLine("    SEQUENCE,")
                .AppendLine("    SEQUENCE OF TEXT,")
                .AppendLine("    enterprises")
                .AppendLine("        FROM SNMPv2-SMI;")
                .AppendLine()
                .AppendLine("END");

            File.WriteAllText(test, builder.ToString());
            var collector = new ErrorRegistry();
            new SimpleObjectRegistry {Tree = {Collector = collector}}
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(test, collector))
                .Refresh();

            Assert.AreEqual(6, collector.Errors.Count);
            foreach (var error in collector.Errors)
            {
                Assert.AreEqual(ErrorCategory.ForbiddenImportedSymbol, error.Category);
            }
#if !TRIAL
            Assert.AreEqual(0, collector.Warnings.Count);
#endif
        }

        [Test]
        public void TestFullNameResolution()
        {
            // IMPORTANT: SMIv2 forbids such imports.
            var test = GetLocation("Import1");
            var builder = new StringBuilder("TEST1-MIB DEFINITIONS ::= BEGIN")
                .AppendLine()
                .AppendLine("IMPORTS")
                .AppendLine("    enterprises")
                .AppendLine("        FROM SNMPv2-SMI;")
                .AppendLine()
                .AppendLine("mytest OBJECT IDENTIFIER ::= { enterprises 9998 }")
                .AppendLine()
                .AppendLine("END");
            File.WriteAllText(test, builder.ToString());

            var test2 = GetLocation("Import2");
            var builder2 = new StringBuilder("TEST2-MIB DEFINITIONS ::= BEGIN")
                .AppendLine()
                .AppendLine("IMPORTS")
                .AppendLine("    enterprises")
                .AppendLine("        FROM SNMPv2-SMI;")
                .AppendLine()
                .AppendLine("mytest OBJECT IDENTIFIER ::= { enterprises 9999 }")
                .AppendLine()
                .AppendLine("END");
            File.WriteAllText(test2, builder2.ToString());

            var test3 = GetLocation("Import3");
            var builder3 = new StringBuilder("TEST3-MIB DEFINITIONS ::= BEGIN")
                .AppendLine()
                .AppendLine("IMPORTS")
                .AppendLine("    mytest")
                .AppendLine("        FROM TEST1-MIB")
                .AppendLine("    mytest")
                .AppendLine("        FROM TEST2-MIB;")
                .AppendLine()
                .AppendLine("mytest1 OBJECT IDENTIFIER ::= { TEST2-MIB.mytest 9999 }")
                .AppendLine()
                .AppendLine("END");
            File.WriteAllText(test3, builder3.ToString());

            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(test, collector))
                .Import(Parser.Compile(test2, collector))
                .Import(Parser.Compile(test3, collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);

#if !TRIAL
            Assert.AreEqual(0, collector.Warnings.Count);
#endif
            {
                uint[] id = registry.Translate("TEST3-MIB::mytest1");
                Assert.AreEqual(new uint[] { 1, 3, 6, 1, 4, 1, 9999, 9999 }, id);
            }
        }


        [Test]
        public void TestFullNameResolution2()
        {
            // IMPORTANT: SMIv2 forbids such imports.
            var test = GetLocation("Import21");
            var builder = new StringBuilder("TEST1-MIB DEFINITIONS ::= BEGIN")
                .AppendLine()
                .AppendLine("IMPORTS")
                .AppendLine("    enterprises")
                .AppendLine("        FROM SNMPv2-SMI;")
                .AppendLine()
                .AppendLine("mytest OBJECT IDENTIFIER ::= { enterprises 9998 }")
                .AppendLine()
                .AppendLine("END");
            File.WriteAllText(test, builder.ToString());

            var test2 = GetLocation("Import22");
            var builder2 = new StringBuilder("TEST2-MIB DEFINITIONS ::= BEGIN")
                .AppendLine()
                .AppendLine("IMPORTS")
                .AppendLine("    enterprises")
                .AppendLine("        FROM SNMPv2-SMI;")
                .AppendLine()
                .AppendLine("mytest OBJECT IDENTIFIER ::= { enterprises 9999 }")
                .AppendLine()
                .AppendLine("END");
            File.WriteAllText(test2, builder2.ToString());

            var test3 = GetLocation("Import23");
            var builder3 = new StringBuilder("TEST3-MIB DEFINITIONS ::= BEGIN")
                .AppendLine()
                .AppendLine("IMPORTS")
                .AppendLine("    mytest")
                .AppendLine("        FROM TEST1-MIB")
                .AppendLine("    mytest")
                .AppendLine("        FROM TEST2-MIB;")
                .AppendLine()
                .AppendLine("mytest1 OBJECT IDENTIFIER ::= { TEST1-MIB.mytest 9999 }")
                .AppendLine()
                .AppendLine("END");
            File.WriteAllText(test3, builder3.ToString());

            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(test, collector))
                .Import(Parser.Compile(test2, collector))
                .Import(Parser.Compile(test3, collector))
                .Refresh();

            Assert.AreEqual(0, collector.Errors.Count);

#if !TRIAL
            Assert.AreEqual(0, collector.Warnings.Count);
#endif
            {
                uint[] id = registry.Translate("TEST3-MIB::mytest1");
                Assert.AreEqual(new uint[] { 1, 3, 6, 1, 4, 1, 9998, 9999 }, id);
            }
        }

        [Test]
        public void TestFullNameResolutionFailed()
        {
            // IMPORTANT: SMIv2 forbids such imports.
            var test = GetLocation("Import11");
            var builder = new StringBuilder("TEST1-MIB DEFINITIONS ::= BEGIN")
                .AppendLine()
                .AppendLine("IMPORTS")
                .AppendLine("    enterprises")
                .AppendLine("        FROM SNMPv2-SMI;")
                .AppendLine()
                .AppendLine("mytest OBJECT IDENTIFIER ::= { enterprises 9998 }")
                .AppendLine()
                .AppendLine("END");
            File.WriteAllText(test, builder.ToString());

            var test2 = GetLocation("Import12");
            var builder2 = new StringBuilder("TEST2-MIB DEFINITIONS ::= BEGIN")
                .AppendLine()
                .AppendLine("IMPORTS")
                .AppendLine("    enterprises")
                .AppendLine("        FROM SNMPv2-SMI;")
                .AppendLine()
                .AppendLine("mytest OBJECT IDENTIFIER ::= { enterprises 9999 }")
                .AppendLine()
                .AppendLine("END");
            File.WriteAllText(test2, builder2.ToString());

            var test3 = GetLocation("Import13");
            var builder3 = new StringBuilder("TEST3-MIB DEFINITIONS ::= BEGIN")
                .AppendLine()
                .AppendLine("IMPORTS")
                .AppendLine("    mytest")
                .AppendLine("        FROM TEST1-MIB")
                .AppendLine("    mytest")
                .AppendLine("        FROM TEST2-MIB;")
                .AppendLine()
                .AppendLine("mytest1 OBJECT IDENTIFIER ::= { mytest 9999 }")
                .AppendLine()
                .AppendLine("END");
            File.WriteAllText(test3, builder3.ToString());

            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(test, collector))
                .Import(Parser.Compile(test2, collector))
                .Import(Parser.Compile(test3, collector))
                .Refresh();

            Assert.AreEqual(1, collector.Errors.Count);
            foreach (var error in collector.Errors)
            {
                Assert.AreEqual(ErrorCategory.DescriptorCollision, error.Category);
            }

#if !TRIAL
            Assert.AreEqual(0, collector.Warnings.Count);
#endif
        }

        // ReSharper restore InconsistentNaming
    }
}
#pragma warning restore 1591
