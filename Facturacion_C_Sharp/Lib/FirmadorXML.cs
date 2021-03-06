﻿using FirmaXadesNet;
using FirmaXadesNet.Crypto;
using FirmaXadesNet.Signature;
using FirmaXadesNet.Signature.Parameters;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace Facturacion_C_Sharp.Lib
{
    public class FirmadorXML
    {
        public static SignatureDocument Firmar ( MensajeReceptor msj, String p12, String password )
        {
            return Firmar( msj.OptenerXML_Nofirmado( ), p12, password );
        }

        public static SignatureDocument Firmar ( Documento doc, String p12, String password )
        {
            return Firmar( doc.OptenerXML_Nofirmado( ), p12, password );
        }

        private static SignatureDocument Firmar ( XDocument docXML, String p12, String password )
        {
            XadesService xadesService = new XadesService( );
            SignatureParameters parametros = new SignatureParameters( );

            // Política de firma de factura-e 3.1
            parametros.SignaturePolicyInfo = new SignaturePolicyInfo( );
            parametros.SignaturePolicyInfo.PolicyIdentifier = "https://www.hacienda.go.cr/ATV/ComprobanteElectronico/docs/esquemas/2016/v4/Resolucion%20Comprobantes%20Electronicos%20%20DGT-R-48-2016.pdf";
            parametros.SignaturePolicyInfo.PolicyHash = "V8lVVNGDCPen6VELRD1Ja8HARFk=";
            parametros.SignaturePackaging = SignaturePackaging.ENVELOPED;
            parametros.DataFormat = new DataFormat( );
            parametros.DataFormat.MimeType = "text/xml";
            parametros.SignerRole = new SignerRole( );
            parametros.SignerRole.ClaimedRoles.Add( "emisor" );
            parametros.Signer = new Signer( new X509Certificate2( p12, password ) );


            Stream stream = new MemoryStream( );
            docXML.Save( stream );
            // Rewind the stream ready to read from it elsewhere
            stream.Position = 0;

            return xadesService.Sign( stream, parametros );
        }
    }
}
