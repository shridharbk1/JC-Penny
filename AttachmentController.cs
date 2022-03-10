using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using IXP.App.BAL.Managers;
using IXP.App.Shared;
using IXP.App.Shared.DTO;
using IXP.App.WebApi.Utility;
using Newtonsoft.Json.Linq;

namespace IXP.App.WebApi.Controllers
{
    /// <summary>
    /// Attachment Controller
    /// </summary>
    /// <seealso cref="IXP.App.WebApi.Controllers.BaseController" />
    [RoutePrefix("attachment")]
    public class AttachmentController : BaseController
    {
        #region Private Fields

        /// <summary>
        /// Master Code Table Manager.
        /// </summary>
        private Lazy<IAttachmentManager> attachmentManager;

        /// <summary>
        /// The logger manager.
        /// </summary>
        private Lazy<ILoggerManager> logger;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentController"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="loggerManager">The logger manager.</param>
        /// <param name="attachmentManager">The attachment manager.</param>
        public AttachmentController(Lazy<IUserManager> userManager, Lazy<ILoggerManager> loggerManager, Lazy<IAttachmentManager> attachmentManager) : base(loggerManager, userManager)
        {
            logger = loggerManager;
            this.attachmentManager = attachmentManager;
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Get by  the attachment identifier.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>HttpResponseMessage Status</returns>
        [HttpGet]
        [Route("byattachmentid/{inquiryId}/{attachmentId}")]
        public async Task<HttpResponseMessage> ByAttachmentID(int inquiryID, int attachmentID)
        {
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            if (declaringType != null)
            {
                string methodInfo = declaringType.FullName;

                try
                {
                    logger.Value.Log(Level.Info, methodInfo);
                    return await GetAttachmentPreview(inquiryID, attachmentID);
                }
                catch (Exception ex)
                {
                    logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Bies the temporary attachment identifier.
        /// </summary>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>Task&lt;HttpResponseMessage&gt;.</returns>
        [HttpGet]
        [Route("byTemporaryAttachmentID/{attachmentId}")]
        public async Task<HttpResponseMessage> ByTemporaryAttachmentID(int attachmentID)
        {
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            if (declaringType != null)
            {
                string methodInfo = declaringType.FullName;

                try
                {
                    logger.Value.Log(Level.Info, methodInfo);
                    return await GetTemporaryAttachmentPreview(attachmentID);
                }
                catch (Exception ex)
                {
                    logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Get by the inquiry identifier.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <returns>List of Attachment</returns>
        [HttpGet]
        [Route("byinquiryid/{inquiryId}")]
        public async Task<List<Attachment>> ByInquiryID(int inquiryID)
        {
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            if (declaringType != null)
            {
                string methodInfo = declaringType.FullName;

                try
                {
                    logger.Value.Log(Level.Info, methodInfo);
                    return await attachmentManager.Value.GetByInquiryID(Convert.ToInt32(inquiryID));
                }
                catch (Exception ex)
                {
                    logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all attachments by inquiry identifier.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <returns>List of Attachments</returns>
        [HttpGet]
        [Route("getAllAttachmentsByInquiryID/{inquiryId}")]
        public async Task<List<Attachment>> GetAllAttachmentsByInquiryID(int inquiryID)
        {
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            if (declaringType != null)
            {
                string methodInfo = declaringType.FullName;

                try
                {
                    logger.Value.Log(Level.Info, methodInfo);
                    var inquiries = await attachmentManager.Value.GetAllAttachmentsByInquiryID(Convert.ToInt32(inquiryID));
                    return inquiries;
                }
                catch (Exception ex)
                {
                    logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                    return null;
                }
            }
            return null;
        }

        [HttpGet]
        [Route("getClosingEmailAttachmentByInquiryID/{inquiryId}")]
        public async Task<List<Attachment>> GetClosingEmailAttachmentByInquiryID(int inquiryID)
        {
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            if (declaringType != null)
            {
                string methodInfo = declaringType.FullName;

                try
                {
                    logger.Value.Log(Level.Info, methodInfo);
                    var inquiries = await attachmentManager.Value.GetClosingEmailAttachmentByInquiryID(Convert.ToInt32(inquiryID));
                    return inquiries;
                }
                catch (Exception ex)
                {
                    logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                    return null;
                }
            }
            return null;
        }


        /// <summary>
        /// Saves the posted file.
        /// </summary>
        /// <param name="localFileId">The local file identifier.</param>
        /// <param name="inquiryId">The inquiry identifier.</param>
        /// <returns>
        /// Number of Rows effected
        /// </returns>
        /// <exception cref="HttpResponseException">Error Response</exception>
        [HttpPost, Route("SavePostedFile/{localFileId}/{inquiryId}")]
        public async Task<List<Attachment>> SavePostedFile(int localFileId, int inquiryId)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;

            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                if (!Request.Content.IsMimeMultipartContent())
                {
                    throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                }

                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                var tempAttachments = await attachmentManager.Value.SavePostedData(provider, this.LoggedInDSID, inquiryId, this.DelegateDSID);
                //Attachment rv = new Attachment();
                //if (tempAttachmentIds.Count > 0)
                //{
                //    rv = await attachmentManager.Value.GetTempAttachmentFileServerInfo(id);

                //    if (rv != null)
                //    {
                //        rv.AttachmentOriginalName = provider.Contents[0].Headers.ContentDisposition.FileName.Trim('\"');
                //        rv.LocalFileId = localFileId;
                //    }
                //}

                return tempAttachments;
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return null;
            }
        }

        [HttpPost, Route("pcaobSavePostedFile/{inquiryId}")]
        public async Task<List<Attachment>> pcaobSavePostedFile(int inquiryId)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;

            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                if (!Request.Content.IsMimeMultipartContent())
                {
                    throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                }

                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                var tempAttachments = await attachmentManager.Value.SavePostedData(provider, this.LoggedInDSID, inquiryId, this.DelegateDSID, true);

                return tempAttachments;
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return null;
            }
        }

        [HttpPost, Route("closingSavePostedFile/{inquiryId}")]
        public async Task<List<Attachment>> closingSavePostedFile(int inquiryId)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;

            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                if (!Request.Content.IsMimeMultipartContent())
                {
                    throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                }

                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                var tempAttachments = await attachmentManager.Value.SavePostedData(provider, this.LoggedInDSID, inquiryId, this.DelegateDSID, isClosingAttachment: true);

                return tempAttachments;
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return null;
            }
        }

        #endregion Public Methods

        /// <summary>
        /// Get by the email identifier.
        /// </summary>
        /// <param name="emailID">The email identifier.</param>
        /// <returns>List of Attachment</returns>
        [HttpGet]
        [Route("byEmailID/{emailID}")]
        public async Task<List<Attachment>> ByEmailID(int emailID)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;

            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                return await attachmentManager.Value.GetByEmailID(Convert.ToInt32(emailID));
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Checks the in attachment.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>
        /// string response
        /// </returns>
        /// <exception cref="HttpResponseException">Error Response</exception>
        [HttpPost, Route("checkinAttachment/{inquiryId}/{attachmentId}/{userId}")]
        public async Task<string> CheckInAttachment(int inquiryID, int attachmentID, int userId)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;

            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                if (!Request.Content.IsMimeMultipartContent())
                {
                    throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                }

                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                return await attachmentManager.Value.CheckInAttachment(provider, inquiryID, attachmentID, userId);
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return ex.Message;
            }
        }

        /// <summary>
        /// Checks the out attachment.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>HttpResponse Status</returns>
        [HttpGet]
        [Route("checkoutAttachment/{inquiryId}/{attachmentId}")]
        public async Task<HttpResponseMessage> CheckOutAttachment(int inquiryID, int attachmentID)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;

            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                var url = ConfigurationManager.AppSettings["FileAccessUrl"];
                using (var client = GetFileAccessSvcClient())
                {
                    var response = await client.GetAsync(url + "/checkout/" + inquiryID + "/" + attachmentID);
                    var responseContent = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    var isOperationComplete = (bool)responseContent["IsOperationComplete"];
                    if (response.IsSuccessStatusCode && isOperationComplete)
                    {
                        return await GetAttachmentFile(inquiryID, attachmentID);
                    }

                    if (!isOperationComplete)
                    {
                        return new HttpResponseMessage(HttpStatusCode.Gone);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Deletes the attachment.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>process status</returns>
        [HttpGet]
        [Route("deleteAttachment/{inquiryId}/{attachmentId}")]
        public async Task<string> DeleteAttachment(int inquiryID, int attachmentID)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            string requestMsDsId = Utils.GetHeader(Request, "ms-dsid");
            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                var data = await attachmentManager.Value.DeleteAttachmentByAttachmentID(inquiryID, attachmentID, requestMsDsId);
                return data.ToString();
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return ex.Message;
            }
        }

        /// <summary>
        /// Deletes the attachment.
        /// </summary>
        /// <param name="attachmentId">The attachment identifier.</param>
        /// <param name="tempAttachmentID">The temporary attachment identifier.</param>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <returns>
        /// process status
        /// </returns>
        [HttpGet]
        [Route("deleteTempPermanentAttachmentById/{attachmentID}/{tempAttachmentID}/{inquiryID}")]
        public async Task<string> deleteTempPermanentAttachmentById(int attachmentId, int tempAttachmentID, int inquiryID)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            string requestMsDsId = Utils.GetHeader(Request, "ms-dsid");
            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                await attachmentManager.Value.DeleteTempPermanentAttachmentById(attachmentId, tempAttachmentID, inquiryID, requestMsDsId, DelegateorLoggedInDSID);
                return "deleted";
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return ex.Message;
            }
        }

        /// <summary>
        /// Deletes the attachment with version.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <param name="version">The version.</param>
        /// <param name="spVersion">The spversion</param>
        /// <returns>HttpResponse Message</returns>
        [HttpGet]
        [Route("deleteAttachmentWithVersion/{inquiryId}/{attachmentId}/{version}/{spVersion}")]
        public async Task<IHttpActionResult> DeleteAttachmentWithVersion(int inquiryID, int attachmentID, int version, int spVersion)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                if (spVersion > 0)
                {
                    return await DeleteAttachmentVersionIdFormSharepointApi(inquiryID, attachmentID, version);
                }

                var result = await attachmentManager.Value.DeleteAttachmentHistory(inquiryID, attachmentID, version);
                if (result > 0)
                {
                    return await DeleteAttachmentVersionIdFormSharepointApi(inquiryID, attachmentID, version);
                }

                return Ok("Inquiry attachment version deleted successfully");
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Deletes the inquiry attachments.
        /// </summary>
        /// <param name="inquiryId">The inquiry identifier.</param>
        /// <returns>
        /// Http response message
        /// </returns>
        [HttpDelete, Route("deletetInquiryAttachments")]
        public async Task<IHttpActionResult> DeleteInquiryAttachments(int inquiryId)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                await attachmentManager.Value.DeleteInquiryAttachments(inquiryId);
                return Ok("All inquiry attachments deleted successfully");
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return BadRequest("Error occurred during deletion of inquiry attachments");
            }
        }

        /// <summary>
        /// Deletes the temporary attachment.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns> string action</returns>
        [HttpPost]
        [Route("deletetempattachment/{id}")]
        public async Task<string> DeleteTempAttachment(int id)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;

            try
            {
                logger.Value.Log(Level.Info, methodInfo);

                await attachmentManager.Value.DeleteTempAttachment(id, Utils.GetHeader(ActionContext.Request, "ms-dsid"));

                return "Deleted";
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return string.Empty;
            }
        }

        public async Task<HttpResponseMessage> GetAttachmentFile(int inquiryID, int attachmentID)
        {
            var data = await attachmentManager.Value.GetAttachmentByAttachmentID(inquiryID, attachmentID);
            try
            {
                if (data != null)
                {
                    try
                    {
                        //This is to create audit log for checkedOut action on attachments
                        string actionDetail = "checked out attachment " + data.DocumentName + " to";
                        await attachmentManager.Value.InsertAuditLogForCheckedOutAttachments(this.LoggedInDSID, inquiryID, actionDetail, null, null, false, null);

                    }
                    catch
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    }
                    if ((data.DocumentData != null && data.DocumentData.Length > 0) && (data.Url == null || data.Url.Trim() == string.Empty))
                    {
                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                        using (var stream = new MemoryStream(data.DocumentData))
                        {
                            result.Content = new ByteArrayContent(stream.ToArray());
                        }

                        result.Content.Headers.Add("x-filename", data.DocumentName);
                        result.Content.Headers.ContentType = new MediaTypeHeaderValue(data.ContentType);
                        result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                        result.Content.Headers.ContentDisposition.FileName = data.DocumentName;

                        return result;
                    }

                    //// if (data.Url != null && data.Url.Trim() != "")
                    var url = ConfigurationManager.AppSettings["FileAccessUrl"];
                    using (var client = GetFileAccessSvcClient())
                    {
                        var httpResponse = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url + "/getfile/" + inquiryID + "/" + attachmentID));
                        if (httpResponse.Result.IsSuccessStatusCode)
                        {
                            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                            using (Stream contentStream = await httpResponse.Result.Content.ReadAsStreamAsync())
                            {
                                using (var stream = new MemoryStream())
                                {
                                    contentStream.CopyTo(stream);
                                    result.Content = new ByteArrayContent(stream.ToArray());
                                }
                            }

                            string fileName = httpResponse.Result.Headers.GetValues("x-http-file-name").FirstOrDefault();
                            result.Content.Headers.Add("x-filename", fileName);
                            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                            result.Content.Headers.ContentDisposition.FileName = fileName;
                            return result;
                        }

                        return null;
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
                ////new HttpResponseMessage(,"Invalid Inquiry id or attachment ID ");
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Gets the attachment file.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>HttpResponseMessage</returns>
        [HttpGet]
        [Route("GetAttachmentFileDownload/{inquiryID}/{attachmentID}")]
        public async Task<HttpResponseMessage> GetAttachmentFileDownload(int inquiryID, int attachmentID)
        {
            var data = await attachmentManager.Value.GetAttachmentByAttachmentID(inquiryID, attachmentID);
            try
            {
                if (data != null)
                {
                    if ((data.DocumentData != null && data.DocumentData.Length > 0) && (data.Url == null || data.Url.Trim() == string.Empty))
                    {
                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                        using (var stream = new MemoryStream(data.DocumentData))
                        {
                            result.Content = new ByteArrayContent(stream.ToArray());
                        }
                        result.Content.Headers.Add("x-filename", data.DocumentName);
                        result.Content.Headers.ContentType = new MediaTypeHeaderValue(data.ContentType);
                        result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                        result.Content.Headers.ContentDisposition.FileName = data.DocumentName;

                        return result;
                    }

                    //// if (data.Url != null && data.Url.Trim() != "")
                    var url = ConfigurationManager.AppSettings["FileAccessUrl"];
                    using (var client = GetFileAccessSvcClient())
                    {
                        var httpResponse = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url + "/getfile/" + inquiryID + "/" + attachmentID));
                        if (httpResponse.Result.IsSuccessStatusCode)
                        {
                            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);

                            using (Stream contentStream = await httpResponse.Result.Content.ReadAsStreamAsync())
                            {
                                using (var stream = new MemoryStream())
                                {
                                    contentStream.CopyTo(stream);
                                    result.Content = new ByteArrayContent(stream.ToArray());
                                }
                            }

                            string fileName = httpResponse.Result.Headers.GetValues("x-http-file-name").FirstOrDefault();
                            result.Content.Headers.Add("x-filename", fileName);
                            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                            result.Content.Headers.ContentDisposition.FileName = fileName;
                            return result;
                        }

                        return null;
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
                ////new HttpResponseMessage(,"Invalid Inquiry id or attachment ID ");
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Gets the attachment history.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>Attachment History</returns>
        [HttpGet]
        [Route("getAttachmentHistory/{inquiryId}/{attachmentId}")]
        public async Task<List<AttachmentHistory>> GetAttachmentHistory(int inquiryID, int attachmentID)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                var data = await attachmentManager.Value.GetAttachmentHistory(inquiryID, attachmentID);
                return data;
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets the attachment with version.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <param name="version">The version.</param>
        /// <returns>HttpResponse Message</returns>
        [HttpGet]
        [Route("downloadAtttachmentWithVersion/{inquiryId}/{attachmentId}/{version}")]
        public async Task<HttpResponseMessage> GetAttachmentWithVersion(int inquiryID, int attachmentID, int version)
        {
            ////[FromBody] dynamic data
            ////var postedData = await Request.Content.ReadAsAsync<JObject>();
            ////int inquiryID=0;
            ////int attachmentID= (int)postedData["attachmentId"];
            ////string url = (string)postedData["url"];//((Newtonsoft.Json.Linq.JValue)(postedData["url"])).Value;
            ////int version=0;
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;

            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                var fileAccessUrl = ConfigurationManager.AppSettings["FileAccessUrl"] + "/GetFileByVersion";
                using (var client = GetFileAccessSvcClient())
                {
                    var postedData = new { InquiryId = inquiryID, AttachmentId = attachmentID, VersionNo = version };
                    var httpResponse = await client.PostAsJsonAsync(fileAccessUrl, postedData);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);

                        using (Stream contentStream = await httpResponse.Content.ReadAsStreamAsync())
                        {
                            using (var stream = new MemoryStream())
                            {
                                contentStream.CopyTo(stream);
                                result.Content = new ByteArrayContent(stream.ToArray());
                            }
                        }

                        string fileName = httpResponse.Headers.GetValues("x-http-file-name").FirstOrDefault();
                        result.Content.Headers.Add("x-filename", fileName);
                        result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                        result.Content.Headers.ContentDisposition.FileName = fileName;
                        return result;
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Gets the email attachment by identifier.
        /// </summary>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>Email Attachment Response</returns>
        [HttpGet]
        [Route("getEmailAttachmentByID/{attachmentId}")]
        public async Task<HttpResponseMessage> GetEmailAttachmentByID(int attachmentID)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;

            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                var data = await attachmentManager.Value.GetEmailAttachmentByID(attachmentID);

                HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                using (var stream = new MemoryStream(data.DocumentData))
                {
                    result.Content = new ByteArrayContent(stream.ToArray());
                }

                result.Content.Headers.Add("x-filename", data.DocumentName);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(data.ContentType);
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                result.Content.Headers.ContentDisposition.FileName = data.DocumentName;

                return result;
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Undoes the attachment.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>HttpResponse Message</returns>
        [HttpGet]
        [Route("undoAttachment/{inquiryId}/{attachmentId}")]
        public async Task<HttpResponseMessage> UndoAttachment(int inquiryID, int attachmentID)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            try
            {
                logger.Value.Log(Level.Info, methodInfo);
                var url = ConfigurationManager.AppSettings["FileAccessUrl"] + "/undocheckout/";
                using (var client = GetFileAccessSvcClient())
                {
                    var response = await client.GetAsync(url + inquiryID + "/" + attachmentID);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                        var isOperationComplete = (bool)responseContent["IsOperationComplete"];
                        if (isOperationComplete)
                        {
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        }

                        return new HttpResponseMessage(HttpStatusCode.Gone);
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                logger.Value.Log(Level.FatalError, methodInfo + " : " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Deletes the attachment version identifier form SharePoint API.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <param name="version">The version.</param>
        /// <returns>Deletion Status</returns>
        private async Task<IHttpActionResult> DeleteAttachmentVersionIdFormSharepointApi(int inquiryID, int attachmentID, int version)
        {
            var url = ConfigurationManager.AppSettings["FileAccessUrl"] + "/DeleteFileByVersion";
            using (var client = GetFileAccessSvcClient())
            {
                // var response = await client.GetAsync(url + inquiryID + "/" + attachmentID);
                var postedData = new { InquiryId = inquiryID, AttachmentId = attachmentID, VersionNo = version };
                var response = await client.PostAsJsonAsync(url, postedData);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    var isOperationComplete = (bool)responseContent["IsOperationComplete"];
                    if (isOperationComplete)
                    {
                        return Ok("Inquiry attachment version deleted successfully");
                    }

                    return BadRequest("Something wrong in sharepoint api");
                }

                return BadRequest("Something went wrong in sharepoint api");
            }
        }

        ///// <summary>
        ///// Gets the attachment file.
        ///// </summary>
        ///// <param name="inquiryID">The inquiry identifier.</param>
        ///// <param name="attachmentID">The attachment identifier.</param>
        ///// <returns>HttpResponse Message</returns>
        //private async Task<HttpResponseMessage> GetAttachmentPreview(int inquiryID, int attachmentID)
        //{
        //    var data = await this.attachmentManager.Value.GetAttachmentByAttachmentID(inquiryID, attachmentID);
        //    string methodInfo = MethodBase.GetCurrentMethod().DeclaringType.FullName;
        //    bool isDownload;
        //    try
        //    {
        //        if (data != null)
        //        {
        //            if ((data.DocumentData != null && data.DocumentData.Length > 0) && (data.Url == null || data.Url.Trim() == string.Empty))
        //            {
        //                var fileExtension = Path.GetExtension(data.DocumentName);
        //                var convertedFileName = string.Empty;
        //                HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
        //                using (var stream = new MemoryStream(data.DocumentData))
        //                {
        //                    var previewhtml = this.GetPreviewDocumentHtml(fileExtension, stream, out isDownload);

        //                    if (isDownload)
        //                    {
        //                        result.Content = new ByteArrayContent(stream.ToArray());
        //                        result.Content.Headers.Add("x-filename", data.DocumentName);
        //                        result.Content.Headers.ContentType = new MediaTypeHeaderValue(data.ContentType);
        //                        result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
        //                        result.Content.Headers.ContentDisposition.FileName = data.DocumentName;
        //                    }
        //                    else
        //                    {
        //                        result.Content = new StringContent(previewhtml);
        //                        result.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
        //                    }
        //                }

        //                return result;
        //            }
        //            else
        //            {
        //                //// if (data.Url != null && data.Url.Trim() != "")
        //                var url = ConfigurationManager.AppSettings["FileAccessUrl"];
        //                using (var client = this.GetFileAccessSvcClient())
        //                {
        //                    var httpResponse = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url + "/getfile/" + inquiryID + "/" + attachmentID));
        //                    if (httpResponse.Result.IsSuccessStatusCode)
        //                    {
        //                        Stream contentStream = await httpResponse.Result.Content.ReadAsStreamAsync();
        //                        string fileName = httpResponse.Result.Headers.GetValues("x-http-file-name").FirstOrDefault();
        //                        var fileExtension = Path.GetExtension(fileName);
        //                        var convertedFileName = string.Empty;
        //                        var stream = new MemoryStream();
        //                        contentStream.CopyTo(stream);
        //                        var previewHtml = this.GetPreviewDocumentHtml(fileExtension, stream, out isDownload);
        //                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
        //                        if (isDownload)
        //                        {
        //                            result.Content = new ByteArrayContent(stream.ToArray());
        //                            result.Content.Headers.Add("x-filename", fileName);

        //                            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
        //                            result.Content.Headers.ContentDisposition.FileName = fileName;
        //                        }
        //                        else
        //                        {
        //                            result.Content = new StringContent(previewHtml);
        //                            result.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
        //                        }

        //                        return result;
        //                    }
        //                    else
        //                    {
        //                        return null;
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        //            ////new HttpResponseMessage(,"Invalid Inquiry id or attachment ID ");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        this.logger.Value.LogException(ex, "{0} : {1}", methodInfo, ex.Message);
        //        return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        //    }
        //}

        /// <summary>
        /// Gets the attachment file.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>HttpResponse Message</returns>
        private async Task<HttpResponseMessage> GetAttachmentPreview(int inquiryID, int attachmentID)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            logger.Value.Log(Level.Info, methodInfo);
            var result = await attachmentManager.Value.GetAttachmentPreviewByAttachmentID(inquiryID, attachmentID);
            logger.Value.Log(Level.Info, $"GetAttachmentPreview - {result.StatusCode}, {result.ReasonPhrase}, {result.IsSuccessStatusCode}");
            bool isDownload;

            try
            {
                if (result.StatusCode.Equals(HttpStatusCode.OK))
                {
                    return result;
                }

                if (result.StatusCode.Equals(HttpStatusCode.Created))
                {
                    //// if (data.Url != null && data.Url.Trim() != "")
                    var url = ConfigurationManager.AppSettings["FileAccessUrl"];
                    using (var client = GetFileAccessSvcClient())
                    {
                        var httpResponse = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url + "/getfile/" + inquiryID + "/" + attachmentID));
                        if (httpResponse.Result.IsSuccessStatusCode)
                        {
                            Stream contentStream = await httpResponse.Result.Content.ReadAsStreamAsync();
                            string fileName = httpResponse.Result.Headers.GetValues("x-http-file-name").FirstOrDefault();
                            var fileExtension = Path.GetExtension(fileName);
                            var convertedFileName = string.Empty;
                            var stream = new MemoryStream();
                            contentStream.CopyTo(stream);
                            var previewHtml = attachmentManager.Value.GetPreviewDocument(fileExtension, stream, out isDownload);
                            HttpResponseMessage resultCreated = new HttpResponseMessage(HttpStatusCode.OK);

                            if (isDownload)
                            {
                                resultCreated.Content = new ByteArrayContent(stream.ToArray());
                                resultCreated.Content.Headers.Add("x-filename", fileName);

                                resultCreated.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                                resultCreated.Content.Headers.ContentDisposition.FileName = fileName;
                            }
                            else
                            {
                                resultCreated.Content = new StringContent(previewHtml);
                                resultCreated.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                            }

                            return resultCreated;
                        }

                        return null;
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
                ////new HttpResponseMessage(,"Invalid Inquiry id or attachment ID ");
            }
            catch (Exception ex)
            {
                logger.Value.LogException(ex, "{0} : {1}", methodInfo, ex.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Gets the attachment file.
        /// </summary>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>HttpResponse Message</returns>
        private async Task<HttpResponseMessage> GetTemporaryAttachmentPreview(int attachmentID)
        {
            var result = await attachmentManager.Value.GetTemporaryAttachmentPreviewByAttachmentID(attachmentID);
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;

            bool isDownload;

            try
            {
                if (result.StatusCode.Equals(HttpStatusCode.OK))
                {
                    return result;
                }

                if (result.StatusCode.Equals(HttpStatusCode.Created))
                {
                    //// if (data.Url != null && data.Url.Trim() != "")
                    var url = ConfigurationManager.AppSettings["FileAccessUrl"];
                    using (var client = GetFileAccessSvcClient())
                    {
                        var httpResponse = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url + "/getfile/" + attachmentID));
                        if (httpResponse.Result.IsSuccessStatusCode)
                        {
                            Stream contentStream = await httpResponse.Result.Content.ReadAsStreamAsync();
                            string fileName = httpResponse.Result.Headers.GetValues("x-http-file-name").FirstOrDefault();
                            var fileExtension = Path.GetExtension(fileName);
                            var convertedFileName = string.Empty;
                            var stream = new MemoryStream();
                            contentStream.CopyTo(stream);
                            var previewHtml = attachmentManager.Value.GetPreviewDocument(fileExtension, stream, out isDownload);
                            HttpResponseMessage resultCreated = new HttpResponseMessage(HttpStatusCode.OK);

                            if (isDownload)
                            {
                                resultCreated.Content = new ByteArrayContent(stream.ToArray());
                                resultCreated.Content.Headers.Add("x-filename", fileName);

                                resultCreated.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                                resultCreated.Content.Headers.ContentDisposition.FileName = fileName;
                            }
                            else
                            {
                                resultCreated.Content = new StringContent(previewHtml);
                                resultCreated.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                            }

                            return resultCreated;
                        }

                        return null;
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
                ////new HttpResponseMessage(,"Invalid Inquiry id or attachment ID ");
            }
            catch (Exception ex)
            {
                logger.Value.LogException(ex, "{0} : {1}", methodInfo, ex.Message);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Updates the temporary attachment comment.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpPost]
        [Route("updateTempAttachmentComment/{id}")]
        public async Task<int> UpdateTempAttachmentComment(int id)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            logger.Value.Log(Level.Info, methodInfo);
            Task<string> comment = ActionContext.Request.Content.ReadAsStringAsync();
            return await attachmentManager.Value.UpdateTempAttachmentComment(id, comment.Result);
        }

        /// <summary>
        /// Updates the post closure attachment comment.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpPost]
        [Route("updatePostClosureAttachmentComment/{id}")]
        public async Task<int> UpdatePostClosureAttachmentComment(int id)
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            logger.Value.Log(Level.Info, methodInfo);
            Task<string> comment = ActionContext.Request.Content.ReadAsStringAsync();
            return await attachmentManager.Value.UpdatePostClosureAttachmentComment(id, comment.Result);
        }

        /// <summary>
        /// Saves the pcaob attachments.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="pcaobReportingField">The pcaob reporting field.</param>
        /// <param name="filesToAdd">The files to add.</param>
        /// <param name="filesToDelete">The files to delete.</param>
        /// <returns>void</returns>
        [HttpPost]
        [Route("savePCAOBAttachments/{inquiryID}/{pcaobReportingField}/{filesToAdd?}/{filesToDelete?}")]
        public async Task SavePCAOBAttachments(int inquiryID, string pcaobReportingField, string filesToAdd = null, string filesToDelete = null)
        {
            await attachmentManager.Value.SavePCAOBReportingAttachments(inquiryID, pcaobReportingField, filesToAdd, filesToDelete, DelegateorLoggedInDSID);
        }

        /// <summary>
        /// Gets the pcaob reporting attachments.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="pcaobReportingField">The pcaob reporting field.</param>
        /// <returns>List of PCAOB reporting attachments.</returns>
        [HttpGet, Route("getPCAOBReportingAttachments/{inquiryID}/{pcaobReportingField}")]
        public async Task<List<Attachment>> GetPCAOBReportingAttachments(int inquiryID, string pcaobReportingField)
        {
            return await attachmentManager.Value.GetPCAOBReportingAttachments(inquiryID, pcaobReportingField);
        }

        /// <summary>
        /// Gets the attachment file.
        /// </summary>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>HttpResponseMessage</returns>
        [HttpGet]
        [Route("GetTemporarayAttachmentFileDownload/{attachmentID}")]
        public async Task<HttpResponseMessage> GetTemporarayAttachmentFileDownload(int attachmentID)
        {
            var data = await attachmentManager.Value.GetTemporaryAttachmentByAttachmentID(attachmentID);
            //var data = await  this.attachmentManager.Value.GetTempAttachmentFileServerInfo(attachmentID);
            try
            {
                if (data != null)
                {
                    if ((data.DocumentData != null && data.DocumentData.Length > 0))
                    {
                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                        using (var stream = new MemoryStream(data.DocumentData))
                        {
                            result.Content = new ByteArrayContent(stream.ToArray());
                        }
                        result.Content.Headers.Add("x-filename", data.DocumentName);
                        result.Content.Headers.ContentType = new MediaTypeHeaderValue(data.ContentType);
                        result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                        result.Content.Headers.ContentDisposition.FileName = data.DocumentName;

                        return result;
                    }

                    return null;
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Downloads the pcaob attachment.
        /// </summary>
        /// <param name="inquiryID">The inquiry identifier.</param>
        /// <param name="pcaobReportingField">The pcaob reporting field.</param>
        /// <param name="attachmentID">The attachment identifier.</param>
        /// <returns>Task&lt;HttpResponseMessage&gt;.</returns>
        [HttpGet]
        [Route("downloadPCAOBAttachmentByID/{inquiryID}/{pcaobReportingField}/{attachmentID}")]
        public async Task<HttpResponseMessage> DownloadPCAOBAttachment(int inquiryID, string pcaobReportingField, int attachmentID)
        {
            var data = await attachmentManager.Value.DownloadPCAOBAttachment(inquiryID, pcaobReportingField, attachmentID);
            try
            {
                if (data != null)
                {
                    if (data.DocumentData != null && data.DocumentData.Length > 0)
                    {
                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                        using (var stream = new MemoryStream(data.DocumentData))
                        {
                            result.Content = new ByteArrayContent(stream.ToArray());
                        }
                        result.Content.Headers.Add("x-filename", data.DocumentName);
                        result.Content.Headers.ContentType = new MediaTypeHeaderValue(data.ContentType);
                        result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                        result.Content.Headers.ContentDisposition.FileName = data.DocumentName;

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Value.LogException(ex, "Error occurred while downloading PCAOB attachmentID:{0} for inquiryID:{1}. Error message: {2}", attachmentID, inquiryID, ex.Message);
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }
}