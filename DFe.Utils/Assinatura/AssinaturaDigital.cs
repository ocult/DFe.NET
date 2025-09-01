﻿/********************************************************************************/
/* Projeto: Biblioteca ZeusDFe                                                  */
/* Biblioteca C# para auxiliar no desenvolvimento das demais bibliotecas DFe    */
/*                                                                              */
/*                                                                              */
/* Direitos Autorais Reservados (c) 2014 Adenilton Batista da Silva             */
/*                                       Zeusdev Tecnologia LTDA ME             */
/*                                                                              */
/*  Você pode obter a última versão desse arquivo no GitHub                     */
/* localizado em https://github.com/adeniltonbs/Zeus.Net.NFe.NFCe               */
/*                                                                              */
/*                                                                              */
/*  Esta biblioteca é software livre; você pode redistribuí-la e/ou modificá-la */
/* sob os termos da Licença Pública Geral Menor do GNU conforme publicada pela  */
/* Free Software Foundation; tanto a versão 2.1 da Licença, ou (a seu critério) */
/* qualquer versão posterior.                                                   */
/*                                                                              */
/*  Esta biblioteca é distribuída na expectativa de que seja útil, porém, SEM   */
/* NENHUMA GARANTIA; nem mesmo a garantia implícita de COMERCIABILIDADE OU      */
/* ADEQUAÇÃO A UMA FINALIDADE ESPECÍFICA. Consulte a Licença Pública Geral Menor*/
/* do GNU para mais detalhes. (Arquivo LICENÇA.TXT ou LICENSE.TXT)              */
/*                                                                              */
/*  Você deve ter recebido uma cópia da Licença Pública Geral Menor do GNU junto*/
/* com esta biblioteca; se não, escreva para a Free Software Foundation, Inc.,  */
/* no endereço 59 Temple Street, Suite 330, Boston, MA 02111-1307 USA.          */
/* Você também pode obter uma copia da licença em:                              */
/* http://www.opensource.org/licenses/lgpl-license.php                          */
/*                                                                              */
/* Zeusdev Tecnologia LTDA ME - adenilton@zeusautomacao.com.br                  */
/* http://www.zeusautomacao.com.br/                                             */
/* Rua Comendador Francisco josé da Cunha, 111 - Itabaiana - SE - 49500-000     */
/********************************************************************************/
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Shared.DFe.Utils;
using SignatureZeus = DFe.Classes.Assinatura.Signature;

namespace DFe.Utils.Assinatura
{
    public class AssinaturaDigital
    {
        public static SignatureZeus Assina<T>(T objeto, string id, X509Certificate2 certificado,
            string signatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1",
            string digestMethod = "http://www.w3.org/2000/09/xmldsig#sha1",
            bool cfgServicoRemoverAcentos = false) where T : class
        {
            var objetoLocal = objeto;
            if (id == null)
                throw new Exception("Não é possível assinar um objeto evento sem sua respectiva Id!");

            var documento = new XmlDocument {PreserveWhitespace = true};

            documento.LoadXml(cfgServicoRemoverAcentos
                ? FuncoesXml.ClasseParaXmlString(objetoLocal).RemoverAcentos()
                : FuncoesXml.ClasseParaXmlString(objetoLocal));

            var docXml = new SignedXml(documento) {SigningKey = certificado.PrivateKey};

            docXml.SignedInfo.SignatureMethod = signatureMethod;
            var reference = new Reference {Uri = "#" + id, DigestMethod = digestMethod};

            // adicionando EnvelopedSignatureTransform a referencia
            var envelopedSigntature = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(envelopedSigntature);

            var c14Transform = new XmlDsigC14NTransform();
            reference.AddTransform(c14Transform);

            docXml.AddReference(reference);

            // carrega o certificado em KeyInfoX509Data para adicionar a KeyInfo
            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(certificado));

            docXml.KeyInfo = keyInfo;
            docXml.ComputeSignature();

            //// recuperando a representacao do XML assinado
            var xmlDigitalSignature = docXml.GetXml();
            var assinatura = FuncoesXml.XmlStringParaClasse<Classes.Assinatura.Signature>(xmlDigitalSignature.OuterXml);
            return assinatura;
        }
        
        /// <summary>
        /// Gera uma assinatura digital no formato PKCS1 utilizando o algoritmo RSA com SHA-1.
        /// </summary>
        /// <param name="certificado">Certificado digital com chave privada usada para assinar.</param>
        /// <param name="valor">Dados a serem assinados em formato de array de bytes.</param>
        /// <returns>Assinatura digital como um array de bytes.</returns>
        public static byte[] CriarAssinaturaPkcs1(X509Certificate2 certificado, byte[] valor)
        {
            var rsa = certificado.GetRSAPrivateKey();

            var rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
            rsaFormatter.SetHashAlgorithm("SHA1");

            var hashSha1Bytes = ObterHashSha1Bytes(valor);
            
            var assinatura = rsaFormatter.CreateSignature(hashSha1Bytes);

            return assinatura;
        }
        
        /// <summary>
        /// Obtém o hash SHA-1 em formato byte array a partir de um array de bytes informado.
        /// </summary>
        public static byte[] ObterHashSha1Bytes(byte[] dados)
        {
            using (var sha1 = SHA1.Create())
            {
                var sha1HashBytes = sha1.ComputeHash(dados);
                
                return sha1HashBytes;
            }
        }

        /// <summary>
        /// Obtém a assinatura do certificado digital no formato PKCS#1, baseado em um array de bytes passado como Argumento [value].
        /// </summary>
        /// <param name="configuracaoCertificado"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ObterAssinaturaPkcs1(ConfiguracaoCertificado configuracaoCertificado, byte[] value)
        {
            X509Certificate2 certificado = CertificadoDigital.ObterCertificado(configuracaoCertificado);
            using (RSA rsa = certificado.GetRSAPrivateKey())
                return rsa.SignData(value, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }
    }
}