﻿using System;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpPro.Mib.Registry;
using System.IO;
using Parser = Lextm.SharpSnmpPro.Mib.Registry.Parser2;
using System.Reflection;
using Lextm.SharpSnmpPro.Mib.Validation;
using Lextm.SharpSnmpLib.Security;
using Mono.Options;
using System.Net.Sockets;
using Lextm.SharpSnmpLib.Messaging;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Lextm.SharpSnmpPro.Mib;

// typical usage
// snmpwalk -c=public -v=1 -m=subtree localhost 1.3.6.1.2.1.1
// snmpwalk -c=public -v=2 -m=subtree -Cr=10 localhost 1.3.6.1.2.1.1
// snmpwalk -v=3 -l=noAuthNoPriv -u=neither -m=subtree -Cr=10 localhost 1.3.6.1.2.1.1
// snmpwalk -v=3 -l=authNoPriv -a=MD5 -A=authentication -u=authen -m=subtree -Cr=10 localhost 1.3.6.1.2.1.1
// snmpwalk -v=3 -l=authPriv -a=MD5 -A=authentication -x=DES -X=privacyphrase -u=privacy -m=subtree -Cr=10 localhost 1.3.6.1.2.1.1

namespace snmpwalk
{
    public static class Program
    {
        private static string GetLocation(string file)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", file);
        }

        public static void Main(string[] args)
        {
            // load MIB documents.
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
            registry.Refresh();
            var tree = registry.Tree;



            // perform MIB assisted WALK operation.
            string community = "public";
            bool showHelp = false;
            bool showVersion = false;
            VersionCode version = VersionCode.V1;
            int timeout = 1000;
            int retry = 0;
            int maxRepetitions = 10;
            Levels level = Levels.Reportable;
            string user = string.Empty;
            string contextName = string.Empty;
            string authentication = string.Empty;
            string authPhrase = string.Empty;
            string privacy = string.Empty;
            string privPhrase = string.Empty;
            WalkMode mode = WalkMode.WithinSubtree;
            bool dump = false;

            OptionSet p = new OptionSet()
                .Add("c:", "Community name, (default is public)", delegate (string v) { if (v != null) community = v; })
                .Add("l:", "Security level, (default is noAuthNoPriv)", delegate (string v)
                {
                    if (v.ToUpperInvariant() == "NOAUTHNOPRIV")
                    {
                        level = Levels.Reportable;
                    }
                    else if (v.ToUpperInvariant() == "AUTHNOPRIV")
                    {
                        level = Levels.Authentication | Levels.Reportable;
                    }
                    else if (v.ToUpperInvariant() == "AUTHPRIV")
                    {
                        level = Levels.Authentication | Levels.Privacy | Levels.Reportable;
                    }
                    else
                    {
                        throw new ArgumentException("no such security mode: " + v);
                    }
                })
                .Add("a:", "Authentication method (MD5 or SHA)", delegate (string v) { authentication = v; })
                .Add("A:", "Authentication passphrase", delegate (string v) { authPhrase = v; })
                .Add("x:", "Privacy method", delegate (string v) { privacy = v; })
                .Add("X:", "Privacy passphrase", delegate (string v) { privPhrase = v; })
                .Add("u:", "Security name", delegate (string v) { user = v; })
                .Add("C:", "Context name", delegate (string v) { contextName = v; })
                .Add("h|?|help", "Print this help information.", delegate (string v) { showHelp = v != null; })
                .Add("V", "Display version number of this application.", delegate (string v) { showVersion = v != null; })
                .Add("d", "Display message dump", delegate (string v) { dump = true; })
                .Add("t:", "Timeout value (unit is second).", delegate (string v) { timeout = int.Parse(v) * 1000; })
                .Add("r:", "Retry count (default is 0)", delegate (string v) { retry = int.Parse(v); })
                .Add("v|version:", "SNMP version (1, 2, and 3 are currently supported)", delegate (string v)
                {
                    if (v == "2c")
                    {
                        v = "2";
                    }

                    switch (int.Parse(v))
                    {
                        case 1:
                            version = VersionCode.V1;
                            break;
                        case 2:
                            version = VersionCode.V2;
                            break;
                        case 3:
                            version = VersionCode.V3;
                            break;
                        default:
                            throw new ArgumentException("no such version: " + v);
                    }
                })
                .Add("m|mode:", "WALK mode (subtree, all are supported)", delegate (string v)
                {
                    if (v == "subtree")
                    {
                        mode = WalkMode.WithinSubtree;
                    }
                    else if (v == "all")
                    {
                        mode = WalkMode.Default;
                    }
                    else
                    {
                        throw new ArgumentException("unknown argument: " + v);
                    }
                })
                .Add("Cr:", "Max-repetitions (default is 10)", delegate (string v) { maxRepetitions = int.Parse(v); });

            if (args.Length == 0)
            {
                ShowHelp(p);
                return;
            }

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return;
            }

            if (extra.Count < 1 || extra.Count > 2)
            {
                Console.WriteLine("invalid variable number: " + extra.Count);
                return;
            }

            if (showVersion)
            {
                Console.WriteLine(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyVersionAttribute>().Version);
                return;
            }

            bool parsed = IPAddress.TryParse(extra[0], out IPAddress ip);
            if (!parsed)
            {
                var addresses = Dns.GetHostAddressesAsync(extra[0]);
                addresses.Wait();
                foreach (IPAddress address in
                    addresses.Result.Where(address => address.AddressFamily == AddressFamily.InterNetwork))
                {
                    ip = address;
                    break;
                }

                if (ip == null)
                {
                    Console.WriteLine("invalid host or wrong IP address found: " + extra[0]);
                    return;
                }
            }

            try
            {
                ObjectIdentifier test = extra.Count == 1 ? new ObjectIdentifier("1.3.6.1.2.1") : new ObjectIdentifier(extra[1]);
                IList<Variable> result = new List<Variable>();
                IPEndPoint receiver = new IPEndPoint(ip, 161);
                if (version == VersionCode.V1)
                {
                    Messenger.Walk(version, receiver, new OctetString(community), test, result, timeout, mode);
                }
                else if (version == VersionCode.V2)
                {
                    Messenger.BulkWalk(version, receiver, new OctetString(community), new OctetString(string.IsNullOrWhiteSpace(contextName) ? string.Empty : contextName), test, result, timeout, maxRepetitions, mode, null, null);
                }
                else
                {
                    if (string.IsNullOrEmpty(user))
                    {
                        Console.WriteLine("User name need to be specified for v3.");
                        return;
                    }

                    IAuthenticationProvider auth = (level & Levels.Authentication) == Levels.Authentication
                        ? GetAuthenticationProviderByName(authentication, authPhrase)
                        : DefaultAuthenticationProvider.Instance;
                    IPrivacyProvider priv;
                    if ((level & Levels.Privacy) == Levels.Privacy)
                    {
                        if (DESPrivacyProvider.IsSupported)
                        {
                            priv = new DESPrivacyProvider(new OctetString(privPhrase), auth);
                        }
                        else
                        {
                            Console.WriteLine("DES (ECB) is not supported by .NET Core.");
                            return;
                        }
                    }
                    else
                    {
                        priv = new DefaultPrivacyProvider(auth);
                    }

                    Discovery discovery = Messenger.GetNextDiscovery(SnmpType.GetBulkRequestPdu);
                    ReportMessage report = discovery.GetResponse(timeout, receiver);
                    Messenger.BulkWalk(version, receiver, new OctetString(user), new OctetString(string.IsNullOrWhiteSpace(contextName) ? string.Empty : contextName), test, result, timeout, maxRepetitions, mode, priv, report);
                }

                foreach (Variable variable in result)
                {
                    var o = tree.Search(variable.Id.ToNumerical());
#if TRIAL
                    Console.WriteLine($"Variable: Id: {o.Text}; Data: {variable.Data}");
#else
                    if (o.Definition.Type == DefinitionType.Scalar || o.Definition.Type == DefinitionType.Column)
                    {
                        var data = registry.Decode(o.Definition.GetNumericalForm(), variable.Data);
                        Console.WriteLine($"Variable: Id: {o.Text}; Data: {data}");
                    }
                    else
                    {
                        Console.WriteLine($"Variable: Id: {o.Text}; Data: {variable.Data}");
                    }
#endif
                }
            }
            catch (SnmpException ex)
            {
                Console.WriteLine(ex);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static IAuthenticationProvider GetAuthenticationProviderByName(string authentication, string phrase)
        {
            if (authentication.ToUpperInvariant() == "MD5")
            {
                return new MD5AuthenticationProvider(new OctetString(phrase));
            }

            if (authentication.ToUpperInvariant() == "SHA")
            {
                return new SHA1AuthenticationProvider(new OctetString(phrase));
            }

            throw new ArgumentException("unknown name", nameof(authentication));
        }

        private static void ShowHelp(OptionSet optionSet)
        {
            Console.WriteLine("#SNMP is available at https://sharpsnmp.com");
            Console.WriteLine("snmpwalk [Options] IP-address|host-name [OID]");
            Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
        }
    }
}
