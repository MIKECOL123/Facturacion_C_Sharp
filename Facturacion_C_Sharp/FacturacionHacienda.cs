﻿using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Facturacion_C_Sharp.Lib;
using Facturacion_C_Sharp.Utils;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace Facturacion_C_Sharp
{
    public class ExecpcionFacturacionHacienda :Exception
    {
        public ExecpcionFacturacionHacienda ( )
        {
        }

        public ExecpcionFacturacionHacienda ( string message )
            : base( message )
        {
        }

        public ExecpcionFacturacionHacienda ( string message, Exception inner )
            : base( message, inner )
        {
        }
    }

    public class ExDocumentoNoAceptado :Exception
    {
        public ExDocumentoNoAceptado ( )
        {
        }

        public ExDocumentoNoAceptado ( string message )
            : base( message )
        {
        }

        public ExDocumentoNoAceptado ( string message, Exception inner )
            : base( message, inner )
        {
        }
    }

    public class FacturacionHacienda
    {
        RestClient restClient = new RestClient( "https://api.comprobanteselectronicos.go.cr/" );

        private String token;

        private Configuracion configuracion;

        //Revisar en caso de error, almacena la ultima respuesta de peticion al server
        private IRestResponse response;

        private String mensajeError;

        public FacturacionHacienda ( Configuracion configuracion )
        {
            this.configuracion = configuracion;
        }

        public void Autenticar ( )
        {
            var request = new RestRequest( configuracion.Authentication_endpoint, Method.POST );
            request.AddParameter( "name", "value" );
            request.AddParameter( "grant_type", "password" );
            request.AddParameter( "username", configuracion.Api_username );
            request.AddParameter( "password", configuracion.Api_password );
            request.AddParameter( "client_id", configuracion.Api_client_id );
            request.AddParameter( "client_secret", "" );
            request.AddParameter( "scope", "" );

            // execute the request
            response = restClient.Execute( request );
            var status = response.StatusCode;

            if( status != System.Net.HttpStatusCode.OK )
            {
                mensajeError = "Err: " + response.ErrorMessage;
                throw new ExecpcionFacturacionHacienda( "Error autentificacion: " + response.ErrorMessage );
            }
            JObject json = JObject.Parse( response.Content );

            token = json[ "access_token" ].ToString( );
        }

        public Configuracion Configuracion
        {
            get => configuracion; set => configuracion = value;
        }
        public IRestResponse Response
        {
            get => response; set => response = value;
        }
        public string MensajeError
        {
            get => mensajeError;
            set => mensajeError = value;
        }

        public bool EnviarDocumento ( Documento documento )
        {
            Autenticar( );
            var request = new RestRequest( configuracion.Documents_endpoint + "/recepcion", Method.POST );
            request.AddHeader( "Authorization", "bearer " + token );

            //request.AddJsonBody(documento.JsonPayload(pathXML).ToString());

            request.AddHeader( "Accept", "application/json" );
            //request.Parameters.Clear();
            request.AddParameter( "application/json", documento.JsonPayload( ).ToString( ), ParameterType.RequestBody );


            // execute the request
            response = restClient.Execute( request );

            if( response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted )
            {
                return true;
            } else
            {
                mensajeError = "Status: " + response.StatusDescription + ", Mensaje: " + response.Content;
                return false;
            }
        }

        public bool EnviarMensajeReceptor ( MensajeReceptor mensajeReceptor )
        {
            Autenticar( );
            var request = new RestRequest( configuracion.Documents_endpoint + "/recepcion", Method.POST );
            request.AddHeader( "Authorization", "bearer " + token );

            //request.AddJsonBody(documento.JsonPayload(pathXML).ToString());

            request.AddHeader( "Accept", "application/json" );
            //request.Parameters.Clear();
            request.AddParameter( "application/json", mensajeReceptor.JsonPayload( ).ToString( ), ParameterType.RequestBody );


            // execute the request
            response = restClient.Execute( request );

            if( response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted )
            {
                return true;
            } else
            {
                string msj = "";
                foreach( var item in response.Headers )
                {
                    if( item.Name == "X-Error-Cause" )
                    {
                        msj = item.Value as string;
                    }
                }
                mensajeError = "Status: " + response.StatusDescription + ", Mensaje: " + msj + ", " + response.Content;
                return false;
            }
        }

        public EstadoDocumento EstadoDocumento ( String claveNumerica )
        {
            Autenticar( );
            var request = new RestRequest( configuracion.Documents_endpoint + "/recepcion/" + claveNumerica, Method.GET );
            request.AddHeader( "Authorization", "bearer " + token );
            request.AddHeader( "content_type", "json" );
            // execute the request
            response = restClient.Execute( request );
            return new EstadoDocumento( response );
        }

        public bool EsValidoElDocumentoContraXSD ( Documento documento )
        {
            var estado = XSDUtils.ValidarXML( documento );
            mensajeError = estado.MensajeError;
            return estado.Valido;
        }

        public string GetMensajeError ( )
        {
            return mensajeError;
        }

        public void GuardarXMLEnviado ( Documento doc, string rutaGuardado = @"DatosXML\Documentos_Enviados\" )
        {
            if( doc.DocumentoFirmado != null )
            {
                Directory.CreateDirectory( rutaGuardado );
                doc.DocumentoFirmado.Save( @rutaGuardado + doc.ClaveNumerica( ) + ".xml" );
            }
        }

        public void GuardarXMLEstado ( EstadoDocumento estado, string rutaGuardado = @"DatosXML\Estados\" )
        {
            if( estado.RepuestaXML != null )
            {
                Directory.CreateDirectory( rutaGuardado );
                estado.RepuestaXML.Document.Save( rutaGuardado + estado.ClaveNumerica + ".xml" );
            }
        }
        public void GuardarXMLMensajeReceptor ( MensajeReceptor mensajeReceptor, string rutaGuardado = @"DatosXML\MensajeReceptor\" )
        {
            if( mensajeReceptor.DocumentoFirmado != null )
            {
                Directory.CreateDirectory( rutaGuardado );
                mensajeReceptor.DocumentoFirmado.Save( @rutaGuardado + mensajeReceptor.IdentificadorGuardado( ) );
            }
        }

        //public EstadoDocumento ObtenerEstadoDocumento(String claveNumerica)
        //{
        //    Autenticar();
        //}
    }
}