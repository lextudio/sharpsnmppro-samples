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
    using Lextm.SharpSnmpLib.Messaging;
    using Registry;
    using System.Collections.Generic;
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
            Assert.That(entity.DescriptionFormatted(), Is.EqualTo("A textual description of the entity.  This value should include the full name and version identification of the system's hardware type, software operating-system, and networking software."));
            Assert.That(entity.Status, Is.EqualTo(EntityStatus.Current));
            Assert.That(entity.Reference, Is.EqualTo(string.Empty));

            var obj = entity as IObjectTypeMacro;
            Assert.That(obj.MibAccess, Is.EqualTo(Access.ReadOnly));
#if TRIAL
            // IMPORTANT: the Trial edition can only show the base syntax.
            Assert.That(obj.BaseSyntax, Is.EqualTo(SnmpType.OctetString));
#endif
#if !TRIAL
            Assert.That(obj.Syntax, Is.TypeOf<ConstraintedType>());

            // IMPORTANT: type resolution shows that OCTET STRING is the base syntax type of DisplayString.
            var constrainted = obj.ResolvedSyntax as ConstraintedType; // Syntax = (DisplayString) + (SIZE (0..255))
            Assert.That(constrainted, Is.Not.Null);
            Assert.That(constrainted.Constraint, Is.Not.Null);

            {
                var specs = constrainted.Constraint.ElementSetSpecs;
                var size = specs.LeftElement.Element as SizeConstraintElement;
                Assert.That(size, Is.Not.Null);
                var range = size.Constraint.ElementSetSpecs.LeftElement.Element as ValueRangeConstraintElement;
                Assert.That(range, Is.Not.Null);
                Assert.That(range.ValueRange.MinValue.ToString(), Is.EqualTo("0"));
                Assert.That(range.ValueRange.MaxValue.ToString(), Is.EqualTo("255"));
            }

            var assignment = constrainted.BaseType as TypeAssignment;
            Assert.That(assignment, Is.Not.Null);

            var textual = assignment.BaseType as TextualConventionMacro; // DisplayString = (OCTET STRING) + (SIZE (0..255))
            Assert.That(textual, Is.Not.Null);

            var constrainted2 = textual.BaseType as ConstraintedType;
            Assert.That(constrainted2, Is.Not.Null);
            Assert.That(constrainted2.Constraint, Is.Not.Null);

            {
                var specs = constrainted2.Constraint.ElementSetSpecs;
                var size = specs.LeftElement.Element as SizeConstraintElement;
                Assert.That(size, Is.Not.Null);
                var range = size.Constraint.ElementSetSpecs.LeftElement.Element as ValueRangeConstraintElement;
                Assert.That(range, Is.Not.Null);
                Assert.That(range.ValueRange.MinValue.ToString(), Is.EqualTo("0"));
                Assert.That(range.ValueRange.MaxValue.ToString(), Is.EqualTo("255"));
            }

            var octet = constrainted2.BaseType as OctetStringType; // OCTET STRING pure type has no constraint.
            Assert.That(octet, Is.Not.Null);

            var type = obj.ResolvedSyntax.GetLastType();
            Assert.That(type, Is.TypeOf<OctetStringType>());

            // IMPORTANT: print out DisplayString syntax as string.
            var name = new StringBuilder();
            obj.ResolvedSyntax.Append(name);
            Assert.That(name.ToString(), Is.EqualTo("DisplayString ::= TEXTUAL-CONVENTION\r\nDISPLAY-HINT \"255a\"\r\nSTATUS current\r\nSYNTAX OCTET STRING"));

            // IMPORTANT: below we test input data against the SIZE constraint of DisplayString.
            Assert.That(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString("test")), Is.True);
            Assert.That(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString(string.Empty)), Is.True);
            Assert.That(registry.Verify("SNMPv2-MIB", "sysDescr", new OctetString(Get257Chars())), Is.False);

            Assert.Throws<InvalidOperationException>(() => registry.Verify(new ObjectIdentifier("1.3.1.6.1.1.2.1.0"), new OctetString("test")));
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
            Assert.That(registry.Verify("IF-MIB", "ifAdminStatus", new Integer32(2)), Is.True);
            Assert.That(registry.Verify("IF-MIB", "ifAdminStatus", new Integer32(5)), Is.False);

            // IMPORTANT: decode the input data to a suitable format.
            Assert.That(registry.Decode("IF-MIB", "ifAdminStatus", new Integer32(2)), Is.EqualTo("down(2)"));

            Assert.Throws<InvalidOperationException>(() => registry.Decode(new ObjectIdentifier("1.2.3"), new Integer32(2)));
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity", new OctetString(new byte[] { 0x8 })), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity", new OctetString(new byte[] { 0x8, 0x9 })), Is.False);
            var bits = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity").DisplayEntity;

            var inner = bits.ResolvedSyntax.GetLastType();
            var inType = inner as OctetStringType;
            Assert.That(inType, Is.Not.Null);
            Assert.That(inType.NamedBits.Count, Is.EqualTo(8));
            var item1 = inType.NamedBits[0] as NamedBit;
            Assert.That(item1.Name, Is.EqualTo("cos0"));
            Assert.That(item1.Number, Is.EqualTo(0));
            var item2 = inType.NamedBits[1] as NamedBit;
            Assert.That(item2.Name, Is.EqualTo("cos1"));
            Assert.That(item2.Number, Is.EqualTo(1));

            // TODO: how to decode BITS?
            // Assert.That(registry.Decode("TEST-MIB", "testEntity", new OctetString(new byte[] { 0x8 })), Is.EqualTo("down(2)"));
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

            Assert.That(registry.Verify("TEST-MIB", "testEntity2", new Integer32(1)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity2", new Integer32(2)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity2", new Integer32(0)), Is.False);
#if !TRIAL
            var entityTruthValue = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity2").DisplayEntity;
            var truthValueName = new StringBuilder();
            entityTruthValue.ResolvedSyntax.Append(truthValueName);
            Assert.That(truthValueName.ToString(), Is.EqualTo("TruthValue ::= TEXTUAL-CONVENTION\r\nSTATUS current\r\nSYNTAX INTEGER { true(1), false(2) }"));

            var inner = entityTruthValue.ResolvedSyntax.GetLastType();
            var inType = inner as IntegerType;
            Assert.That(inType, Is.Not.Null);
            Assert.That(inType.NamedNumberList.Count, Is.EqualTo(2));
            var item1 = inType.NamedNumberList[0] as NamedNumber;
            Assert.That(item1.Name, Is.EqualTo("true"));
            Assert.That((item1.Value as NumberLiteralValue).Value, Is.EqualTo(1));
            var item2 = inType.NamedNumberList[1] as NamedNumber;
            Assert.That(item2.Name, Is.EqualTo("false"));
            Assert.That((item2.Value as NumberLiteralValue).Value, Is.EqualTo(2));
#endif

            Assert.That(registry.Decode("TEST-MIB", "testEntity2", new Integer32(1)), Is.EqualTo("true(1)"));
            Assert.That(registry.Decode("TEST-MIB", "testEntity2", new Integer32(2)), Is.EqualTo("false(2)"));
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9 })), Is.False);

            Assert.That(registry.Decode("TEST-MIB", "testEntity3", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x10 })), Is.EqualTo("09-09-09-09-09-10"));
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity4", new Integer32(1)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity4", new Integer32(2)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity4", new Integer32(3)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity4", new Integer32(4)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity4", new Integer32(5)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity4", new Integer32(6)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity4", new Integer32(0)), Is.False);
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })), Is.True);
            Assert.Throws<InvalidOperationException>(() => registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9, 0x9 })));
            Assert.That(registry.Verify("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x9 })), Is.False);

            Assert.That(registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xD4, 0x08, 0x11, 0x0F, 0x30, 0x00, 0x00, 0x2D, 0x05, 0x00 })), Is.EqualTo("2004-08-17T15:48:00.0000000-05:00"));
            Assert.That(registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xD4, 0x08, 0x11, 0x0F, 0x30, 0x00, 0x00 })), Is.EqualTo("2004-08-17T15:48:00.0000000+00:00"));
            Assert.That(registry.Decode("TEST-MIB", "testEntity5", new OctetString(new byte[] { 0x07, 0xC8, 5, 26, 13, 30, 15, 0x00, 0x2D, 0x04, 0x00 })), Is.EqualTo("1992-05-26T13:30:15.0000000-04:00"));
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity6", new Integer32(1)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity6", new Integer32(2)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity6", new Integer32(3)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity6", new Integer32(4)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity6", new Integer32(5)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity6", new Integer32(0)), Is.False);
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity7", new OctetString(new byte[0])), Is.False);
            Assert.That(registry.Verify("TEST-MIB", "testEntity7", new OctetString(new byte[] { 0x9 })), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity7", new OctetString(Get257Chars())), Is.False);
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity8", new Integer32(0)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity8", new Integer32(254)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity8", new Integer32(255)), Is.False);
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[0])), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[] { 0x9 })), Is.False);
            Assert.That(registry.Verify("TEST-MIB", "testEntity9", new OctetString(new byte[] { 0x9, 0x9 })), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity9", new OctetString(Get257Chars())), Is.False);
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(0)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(255)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity10", new Gauge32(256)), Is.False);
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity11", new Integer32(1)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity11", new Integer32(2)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity11", new Integer32(3)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity11", new Integer32(4)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity11", new Integer32(0)), Is.False);
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity12", new Integer32(0)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity12", new Integer32(65535)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity12", new Integer32(65536)), Is.False);
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
            Assert.That(registry.Verify("TEST-MIB", "testEntity13", new Integer32(0)), Is.False);
            Assert.That(registry.Verify("TEST-MIB", "testEntity13", new Integer32(30000000)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity13", new Integer32(31010000)), Is.False);
            Assert.That(registry.Verify("TEST-MIB", "testEntity13", new Integer32(13750000)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity13", new Integer32(14510000)), Is.False);
            Assert.That(registry.Verify("TEST-MIB", "testEntity13", new Integer32(5850000)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity13", new Integer32(6425100)), Is.False);
            Assert.That(registry.Verify("TEST-MIB", "testEntity13", new Integer32(7900000)), Is.True);
            Assert.That(registry.Verify("TEST-MIB", "testEntity13", new Integer32(8401000)), Is.False);
        }

        private static ObjectRegistryBase LoadTestingDocuments()
        {
            var collector = new ErrorRegistry();
            return new SimpleObjectRegistry { Tree = { Collector = collector } }
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
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("Test1.mib"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
            Assert.That(collector.Warnings.Count, Is.EqualTo(2));

#if !TRIAL
                var choiceValue = (ObjectTypeMacro)registry.Tree.Find("TEST-MIB", "testEntity14").DisplayEntity;
                var resolvedSyntax = choiceValue.ResolvedSyntax;
                var inner = resolvedSyntax.GetLastType();
                Assert.That(inner.Name, Is.EqualTo("NetworkAddress"));
                var inType = inner as ChoiceType;
                Assert.That(inType, Is.Not.Null);

                // IMPORTANT: This list only contains one element.
                Assert.That(inType.ElementTypes.Count, Is.EqualTo(1));
                var item1 = inType.ElementTypes[0] as TaggedElementType;
                Assert.That(item1.Name, Is.EqualTo("internet"));
                var root = item1.Subtype.GetLastType();

                // IMPORTANT: The type of this element is IpAddress.
                Assert.That(root, Is.TypeOf<IpAddressType>());
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
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Refresh();
            Assert.That(registry.ValidateTable(table), Is.True);
            Assert.That(registry.ValidateTable(entry), Is.False);
            Assert.That(registry.ValidateTable(unknown), Is.False);
        }

        [Test]
        public void TestGetTextualForms()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
            Assert.That(collector.Warnings.Count, Is.EqualTo(0));
            const string iso = "::iso";
            Assert.That(registry.Translate(new uint[] { 1 }), Is.EqualTo(iso));
            Assert.That(registry.Translate(iso), Is.EqualTo(new uint[] { 1 }));
            const string transmission = "SNMPv2-SMI::transmission";
            Assert.That(registry.Translate(new uint[] { 1, 3, 6, 1, 2, 1, 10 }), Is.EqualTo(transmission));
            Assert.That(registry.Translate(transmission), Is.EqualTo(new uint[] { 1, 3, 6, 1, 2, 1, 10 }));

            Assert.That(registry.Translate(new uint[] { 1, 3, 6, 1, 2, 1, 1 }), Is.EqualTo("SNMPv2-MIB::system"));
            const string domain = "SNMPv2-TM::snmpUDPDomain";
            Assert.That(registry.Translate(ObjectIdentifier.AppendTo(registry.Translate("SNMPv2-SMI::snmpDomains"), 1)), Is.EqualTo(domain));
            Assert.That(registry.Translate(domain), Is.EqualTo(new uint[] { 1, 3, 6, 1, 6, 1, 1 }));

            Assert.That(registry.Translate("::ccitt"), Is.EqualTo(new uint[] { 0 }));
            const string zero = "SNMPv2-SMI::zeroDotZero";
            Assert.That(registry.Translate(new uint[] { 0, 0 }), Is.EqualTo(zero));
            Assert.That(registry.Translate(zero), Is.EqualTo(new uint[] { 0, 0 }));

            var item = registry.Tree.Find("SNMPv2-SMI", "zeroDotZero");
            Assert.That(item.DisplayEntity.GetObjectIdentifier(), Is.EqualTo(new uint[] { 0, 0 }));

            Assert.That(registry.Translate("SNMPv2-MIB::system"), Is.EqualTo(new uint[] { 1, 3, 6, 1, 2, 1, 1 }));
        }

        [Test]
        public void TestsysORTable()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
            Assert.That(collector.Warnings.Count, Is.EqualTo(0));

            uint[] id = registry.Translate("SNMPv2-MIB::sysORTable");
#if !TRIAL
            Assert.That(registry.ValidateTable(new ObjectIdentifier(id)), Is.True);
#endif
            var node = registry.Tree.Find("SNMPv2-MIB", "sysORTable");
            var node1 = registry.Tree.Find("SNMPv2-MIB", "sysOREntry");
            var node2 = registry.Tree.Find("SNMPv2-MIB", "sysORIndex");
            Assert.That(node.Type, Is.EqualTo(DefinitionType.Table));
            Assert.That(node1.Type, Is.EqualTo(DefinitionType.Entry));
            Assert.That(node2.Type, Is.EqualTo(DefinitionType.Column));

            Assert.That(registry.Translate("SNMPv2-MIB::sysORTable.0"), Is.EqualTo(new uint[] { 1, 3, 6, 1, 2, 1, 1, 9, 0 }));
            Assert.That(registry.Translate(new uint[] { 1, 3, 6, 1, 2, 1, 1, 9, 0 }), Is.EqualTo("SNMPv2-MIB::sysORTable.0"));

#if !TRIAL
            Assert.That(registry.ValidateTable(new ObjectIdentifier(registry.Translate("SNMPv2-MIB::snmpMIB"))), Is.False);
#endif
        }

        [Test]
        public void TestActona()
        {
            const string name = "ACTONA-ACTASTOR-MIB::actona";
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(GetLocation("ACTONA-ACTASTOR-MIB.mib"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
            Assert.That(collector.Warnings.Count, Is.EqualTo(0));

            uint[] id = registry.Translate(name);

            Assert.That(id, Is.EqualTo(new uint[] { 1, 3, 6, 1, 4, 1, 17471 }));
            Assert.That(registry.Translate(id), Is.EqualTo(name));
        }

        [Test]
        public void TestIEEE802dot11_MIB()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
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

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(150));
#endif
            const string notification = "IEEE802dot11-MIB::dot11SMTnotification";
            Assert.That(registry.Translate(new uint[] { 1, 2, 840, 10036, 1, 6 }), Is.EqualTo(notification));
            uint[] id = registry.Translate(notification);
            Assert.That(id, Is.EqualTo(new uint[] { 1, 2, 840, 10036, 1, 6 }));

            const string name1 = "IEEE802dot11-MIB::dot11Disassociate";
            var id1 = new uint[] { 1, 2, 840, 10036, 1, 6, 0, 1 };
            Assert.That(registry.Translate(name1), Is.EqualTo(id1));
            Assert.That(registry.Translate(id1), Is.EqualTo(name1));
        }

        [Test]
        public void TestJVM_MANAGEMENT_MIB()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
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

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(151));
#endif
            const string jmgt = "JVM-MANAGEMENT-MIB::jmgt";
            Assert.That(registry.Translate(new uint[] { 1, 3, 6, 1, 4, 1, 42, 2, 145 }), Is.EqualTo(jmgt));
            uint[] id = registry.Translate(jmgt);
            Assert.That(id, Is.EqualTo(new uint[] { 1, 3, 6, 1, 4, 1, 42, 2, 145 }));

            var item = registry.Tree.Find("JVM-MANAGEMENT-MIB", "jmgt");
            Assert.That(item.DisplayEntity.GetObjectIdentifier(), Is.EqualTo(new uint[] { 1, 3, 6, 1, 4, 1, 42, 2, 145 }));
        }


        [Test]
        public void TestALLIEDTELESYN_MIB()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry
            {
                Tree = { Collector = collector, PendingModulesAllowed = true }
            }
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
            Assert.That(collector.Warnings.Count, Is.EqualTo(149));
#endif
            Assert.That(collector.Errors.Count, Is.EqualTo(1));
            var o = registry.Tree.Search(ObjectIdentifier.Convert("3.6.1.2.1.25"));
            Assert.That(o.Definition, Is.Null);
            Assert.That(o.AlternativeText, Is.EqualTo(".3.6.1.2.1.25"));
            Assert.That(o.Text, Is.EqualTo(".3.6.1.2.1.25"));
            Assert.That(collector.Errors.Count, Is.EqualTo(1));
            Assert.That(collector.Errors.ElementAt(0).Category, Is.EqualTo(ErrorCategory.MissingDependency));
        }

        [Test]
        public void TestHOSTRESOURCES_MIB()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("HOST-RESOURCES-MIB.txt"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(0));
#endif
            var module = registry.Tree.LoadedModules.First(mod => mod.Name == "HOST-RESOURCES-MIB");
            Assert.That(module.Objects.Count, Is.EqualTo(83));
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
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IEEE8021-TC-MIB.txt"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
            Assert.That(collector.Warnings.Count, Is.EqualTo(14));

            var zero = registry.Translate(new uint[] { 0, 0 });
            Assert.That(zero, Is.EqualTo("SNMPv2-SMI::zeroDotZero"));

            var module = registry.Tree.LoadedModules.FirstOrDefault(mod => mod.Name == "IEEE8021-TC-MIB");
            Assert.That(module.Objects.Count, Is.EqualTo(0));
            var child = registry.Translate("IEEE8021-TC-MIB::ieee8021TcMib");
            Assert.That(ObjectIdentifier.Convert(child), Is.EqualTo("1.3.111.2.802.1.1.1"));
            var parent = registry.Translate("IEEE8021-TC-MIB::ieee802dot1mibs");
            Assert.That(ObjectIdentifier.Convert(parent), Is.EqualTo("1.3.111.2.802.1.1"));

            // IMPORTANT: assistant OIDs were utilized in 1.1.1 and older releases to support such scenarios. They are no longer required in 1.1.2 and above.
            Assert.That(registry.Tree.Find("IEEE8021-TC-MIB", "ieee802dot1_1"), Is.Null);

            var definition = registry.Tree.Find("IEEE8021-TC-MIB", "ieee802dot1mibs");
            // IMPORTANT: since no more assistant OIDs exists, the textual form is now unique.
            Assert.That(definition.TextualForms.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestDLSW()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("DLSW-MIB.my"), collector))
                .Import(Parser.Compile(GetLocation("SNA-SDLC-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("RFC-1212"), collector))
                .Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("RFC1213-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("RFC1271-MIB.mib"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(253));
#endif
            var definition = registry.Tree.Find("DLSW-MIB", "null");
            Assert.That(definition.DisplayEntity.Name, Is.EqualTo("zeroDotZero"));
        }

        [Test]
        public void TestFoundry()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
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

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(10));
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
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMP-FRAMEWORK-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMP-TARGET-MIB.txt"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
            Assert.That(collector.Warnings.Count, Is.EqualTo(0));

            var definition = registry.Tree.Find("SNMP-TARGET-MIB", "snmpTargetAddrEntry");
            var type = definition.DisplayEntity as IObjectTypeMacro;
            Assert.That(type, Is.Not.Null);
#if !TRIAL
            var real = (ObjectTypeMacro)type;
            Assert.That(real.IndexList.Count, Is.EqualTo(1));
            var index = real.IndexList[0];
            Assert.That(index.Type.Name, Is.EqualTo("snmpTargetAddrName"));
            Assert.That(index.Implied, Is.True);
#endif
        }

        [Test]
        public void TestAugments()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
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
#if !TRIAL
            Assert.That(type, Is.InstanceOf<ObjectTypeMacro>());
            var real = (ObjectTypeMacro)type;
            Assert.That(real.Augments, Is.Not.Null);

            Assert.That(real.Augments.Type.Name, Is.EqualTo("ifEntry"));
            Assert.That(real.Augments.Type.Module.Name, Is.EqualTo("IF-MIB"));
            Assert.That(real.Augments.Type.ResolvedSyntax.GetLastType(), Is.TypeOf<SequenceType>());
#endif
        }

        [Test]
        public void TestDuplicateModule()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry
            {
                Tree = { Collector = collector, PendingModulesAllowed = true }
            }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("empty.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(4));
            Assert.That(collector.Warnings.Count, Is.EqualTo(0));

            var item = collector.Errors.ElementAt(0);
#if !TRIAL
            Assert.That(item.Category, Is.EqualTo(ErrorCategory.DuplicateModule));
#endif
        }

        [Test]
        public void TestInvalidDocument()
        {
            var file = GetLocation("Invalid.txt");
            var collector = new ErrorRegistry();
            new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(file, collector))
                .Refresh();
            Assert.That(collector.Errors.Count, Is.EqualTo(1));

            var item = collector.Errors.ElementAt(0);
            Assert.That(item.Category, Is.EqualTo(ErrorCategory.SematicError));
            Assert.That(item.ToString(), Is.EqualTo($"{file} (1,6) : error S0001 : Invalid token 'is'."));
        }

        [Test]
        public void TestDocsCableDeviceTrapMib()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
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

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(2));
#endif
            var definition = registry.Tree.Find("DOCS-CABLE-DEVICE-TRAP-MIB", "docsDevCmInitTLVUnknownTrap");
            Assert.That(registry.Translate(definition.GetNumericalForm()), Is.EqualTo("DOCS-CABLE-DEVICE-TRAP-MIB::docsDevCmInitTLVUnknownTrap"));
#if !TRIAL
            var type = definition.DisplayEntity as NotificationTypeMacro;
            Assert.That(type, Is.Not.Null);
#endif
        }

        [Test]
        public void TestSonicWallFirewallTrapMibMib()
        {
            var collector = new ErrorRegistry();
            new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SONICWALL-SMI.mib"), collector))
                .Import(Parser.Compile(GetLocation("SONICWALL-FIREWALL-TRAP-MIB.mib"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
            Assert.That(collector.Warnings.Count, Is.EqualTo(1));
            var warning = collector.Warnings.First();
            Assert.That(warning.Category, Is.EqualTo(WarningCategory.ImplicitNodeCreation));
        }

        [Test]
        public void TestADSL()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
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

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(1));
#endif
            uint[] id = registry.Translate("ADSL-TC-MIB::adsltcmib");
            Assert.That(id, Is.Null);
            // IMPORTANT: The module is not pending, but its contents cannot be put on to the tree.
            // Assert.That(id, Is.EqualTo(new uint[] { 1, 3, 6, 1, 4, 1, 17471 }));
            // Assert.That(registry.Translate(id), Is.EqualTo("ADSL-TC-MIB::adsltcmib"));
        }

        [Test]
        public void TestIEEE8023LAG_MIB()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
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

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(461));
#endif
            {
                const string lag = "IEEE8023-LAG-MIB::lagMIB";
                Assert.That(registry.Translate(new uint[] { 1, 2, 840, 10006, 300, 43 }), Is.EqualTo(lag));
                uint[] id = registry.Translate(lag);
                Assert.That(id, Is.EqualTo(new uint[] { 1, 2, 840, 10006, 300, 43 }));
            }
            {
                const string dot = "IEEE8023-LAG-MIB::802dot3";
                Assert.That(registry.Translate(new uint[] { 1, 2, 840, 10006 }), Is.EqualTo(dot));
                uint[] id = registry.Translate(dot);
                Assert.That(id, Is.EqualTo(new uint[] { 1, 2, 840, 10006 }));
            }

            {
                var module = registry.Tree.LoadedModules.First(mod => mod.Name == "BRIDGE-MIB");
                Assert.That(module.Entities.Count(_ => _ is TrapTypeMacro), Is.EqualTo(2));

                const string trap1 = "BRIDGE-MIB::newRoot";
                Assert.That(registry.Translate(new uint[] { 1, 3, 6, 1, 2, 1, 17, 0, 1 }), Is.EqualTo(trap1));
                var item = registry.Tree.Find("BRIDGE-MIB", "newRoot");
                Assert.That(item.DisplayEntity is TrapTypeMacro);
            }
        }

        [Test]
        public void TestCisco()
        {
            var collector = new ErrorRegistry();
            new SimpleObjectRegistry { Tree = { Collector = collector } }
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

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(2));
#endif
            foreach (var warning in collector.Warnings)
            {
                Assert.That(warning.Category, Is.EqualTo(WarningCategory.WrongIndexType));
            }
        }

        [Test]
        public void TestCiscoISDN()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry { Tree = { Collector = collector } }
                .Import(Parser.Compile(GetLocation("RFC1155-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("RFC-1212"), collector))
                .Import(Parser.Compile(GetLocation("RFC1213-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
                .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("INET-ADDRESS-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
                .Import(Parser.Compile(GetLocation("CISCO-SMI.mib"), collector))
                .Import(Parser.Compile(GetLocation("ISDN-MIB.mib"), collector))
                .Import(Parser.Compile(GetLocation("CISCO-ISDN-MIB.mib"), collector))
                .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(149));

            Assert.That(collector.Warnings.Count(w => w.Category == WarningCategory.ImplicitNodeCreation), Is.EqualTo(2));
            Assert.That(collector.Warnings.Count(w => w.Category == WarningCategory.SematicError), Is.EqualTo(147));
#endif

            var trap = registry.Tree.Find("CISCO-ISDN-MIB", "demandNbrLayer2Change").DisplayEntity;
            var message = new TrapV2Message(
                5001,
                VersionCode.V2,
                new OctetString("public"),
                new ObjectIdentifier(trap.GetObjectIdentifier()),
                500,
                new List<Variable>
                {
                    new Variable(new ObjectIdentifier(registry.Translate("RFC1213-MIB::ifIndex")), new Integer32(2)),
                    new Variable(new ObjectIdentifier(registry.Translate("ISDN-MIB::isdnLapdOperStatus")), new Integer32(3))
                });

            Assert.That(registry.Translate(message.Enterprise.ToNumerical()), Is.EqualTo("CISCO-ISDN-MIB::demandNbrLayer2Change"));
            Assert.That(registry.Translate(message.Variables()[0].Id.ToNumerical()), Is.EqualTo("RFC1213-MIB::ifIndex"));
            Assert.That(registry.Translate(message.Variables()[1].Id.ToNumerical()), Is.EqualTo("ISDN-MIB::isdnLapdOperStatus"));
#if !TRIAL
            var status = (ObjectTypeMacro)registry.Tree.Find("ISDN-MIB", "isdnLapdOperStatus").DisplayEntity;
            var syntax = (IntegerType)status.ResolvedSyntax;
            var nameList = syntax.NamedNumberList;
            foreach (NamedNumber namedNumber in nameList)
            {
                if (((NumberLiteralValue)namedNumber.Value).Value == ((Integer32)message.Variables()[1].Data).ToInt32())
                {
                    Assert.That(namedNumber.Name, Is.EqualTo("l2Active"));
                }
            }
#endif
        }

        [Test]
        public void TestPendingDocuments()
        {
            var collector = new ErrorRegistry();
            var registry = new SimpleObjectRegistry
            {
                Tree =
                {
                    Collector = collector,
                    PendingModulesAllowed = true
                }
            }
            .Import(Parser.Compile(GetLocation("IANAifType-MIB.txt"), collector))
            .Import(Parser.Compile(GetLocation("INET-ADDRESS-MIB.txt"), collector))
            .Import(Parser.Compile(GetLocation("IF-MIB.txt"), collector))
            .Import(Parser.Compile(GetLocation("CISCO-SMI.mib"), collector))
            .Import(Parser.Compile(GetLocation("ISDN-MIB.mib"), collector))
            .Import(Parser.Compile(GetLocation("CISCO-ISDN-MIB.mib"), collector))
            .Refresh();

            // IMPORTANT: Verify the collected errors and see why MIB documents are pending.
            Assert.That(collector.Errors.Count, Is.GreaterThan(0));
            Assert.That(registry.Tree.PendingModules.Count, Is.EqualTo(6));
        }

        [Test]
        public void TestInvalidImport()
        {
            // IMPORTANT: SMIv2 forbids such imports. RFC2578 3.2.
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
            new SimpleObjectRegistry
            {
                Tree = { Collector = collector, PendingModulesAllowed = true }
            }
            .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
            .Import(Parser.Compile(test, collector))
            .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(6));
            foreach (var error in collector.Errors)
            {
                Assert.That(error.Category, Is.EqualTo(ErrorCategory.ForbiddenImportedSymbol));
            }
#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(0));
#endif
        }

        [Test]
        public void TestFullNameResolution()
        {
            // IMPORTANT: RFC2578 3.2.
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
            var registry = new SimpleObjectRegistry
            {
                Tree = { Collector = collector, PendingModulesAllowed = true }
            }
            .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
            .Import(Parser.Compile(test, collector))
            .Import(Parser.Compile(test2, collector))
            .Import(Parser.Compile(test3, collector))
            .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(0));

#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(0));
#endif
            {
                uint[] id = registry.Translate("TEST3-MIB::mytest1");
                Assert.That(id, Is.EqualTo(new uint[] { 1, 3, 6, 1, 4, 1, 9999, 9999 }));
            }
        }


        [Test]
        public void TestFullNameResolution2()
        {
            // IMPORTANT: RFC2578 3.2.
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

            Assert.That(collector.Errors.Count, Is.EqualTo(0));

#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(0));
#endif
            {
                uint[] id = registry.Translate("TEST3-MIB::mytest1");
                Assert.That(id, Is.EqualTo(new uint[] { 1, 3, 6, 1, 4, 1, 9998, 9999 }));
            }
        }

        [Test]
        public void TestFullNameResolutionFailed()
        {
            // IMPORTANT: RFC2578 3.2.
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
            var registry = new SimpleObjectRegistry
            {
                Tree = { Collector = collector, PendingModulesAllowed = true }
            }
            .Import(Parser.Compile(GetLocation("SNMPv2-SMI.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-CONF.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-TC.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-MIB.txt"), collector))
            .Import(Parser.Compile(GetLocation("SNMPv2-TM.txt"), collector))
            .Import(Parser.Compile(test, collector))
            .Import(Parser.Compile(test2, collector))
            .Import(Parser.Compile(test3, collector))
            .Refresh();

            Assert.That(collector.Errors.Count, Is.EqualTo(1));
            foreach (var error in collector.Errors)
            {
                Assert.That(error.Category, Is.EqualTo(ErrorCategory.DescriptorCollision));
            }

#if !TRIAL
            Assert.That(collector.Warnings.Count, Is.EqualTo(0));
#endif
        }

        // ReSharper restore InconsistentNaming
    }
}
#pragma warning restore 1591
