﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;
using System.Net;
using System.ServiceModel.Description;
using Microsoft.Xrm.Sdk.Query;
using HoloDynamicsAPI.Models;



namespace HoloDynamicsAPI
{
    public class OrganizationMan
    {
        public IOrganizationService organizationService = null;
        public Guid userid = new Guid();
        private List<Product> productList = new List<Product>();

        public void ConnectToCrm(string username, string password)
        {
            if(username != null && password != null)
            {
                ClientCredentials clientCredentials = new ClientCredentials();
                clientCredentials.UserName.UserName = username;
                clientCredentials.UserName.Password = password;

                // For Dynamics 365 Customer Engagement V9.X, set Security Protocol as TLS12
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                // Get the URL from CRM, Navigate to Settings -> Customizations -> Developer Resources
                // Copy and Paste Organization Service Endpoint Address URL
                organizationService = (IOrganizationService)new OrganizationServiceProxy(new Uri("https://scapta.api.crm4.dynamics.com/XRMServices/2011/Organization.svc"),
                 null, clientCredentials, null);

                if (organizationService != null)
                {
                    userid = ((WhoAmIResponse)organizationService.Execute(new WhoAmIRequest())).UserId;
                }
                else
                {
                    Console.WriteLine("Failed to Established Connection!!!");
                }
            }        
        }

        public List<Product> getProducts()
        {
            if (userid != Guid.Empty)
            {
                int i = 0;
                QueryExpression productQuery = new QueryExpression
                {
                    EntityName = "scp_holoproduct",
                    ColumnSet = new ColumnSet(true)
                };
                DataCollection<Entity> products = organizationService.RetrieveMultiple(productQuery).Entities;

                
                foreach (Entity entity in products)
                {
                    Product prod = new Product();
                    prod.productId = entity.Attributes["scp_holoproductid"].ToString();
                    prod.productNaam = entity.Attributes["scp_name"].ToString();
                    prod.productLogo = entity.Attributes["scp_logourl"].ToString();

                    EntityReference marketinglist = entity.Attributes["scp_marketinglist"] as EntityReference;
                    prod.marketinglist = marketinglist;

                    var id = marketinglist.Id;

                    var mark = entity.Attributes["scp_marketinglist"];

                    productList.Add(prod);
                    i++;
                }
            }
            return productList; 
        }

        public List<Account> getCustomersByProductId(string productId)
        {
            List<Account> customerList = new List<Account>();
            List<Guid> memberGuids = new List<Guid>();
            if (userid != Guid.Empty)
            {
                Entity product = organizationService.Retrieve("scp_holoproduct", new Guid(productId), new ColumnSet(true));
                EntityReference markList = product.Attributes["scp_marketinglist"] as EntityReference;

                QueryByAttribute query = new QueryByAttribute("listmember");
                query.AddAttributeValue("listid", markList.Id);
                query.ColumnSet = new ColumnSet(true);
                EntityCollection listMembers = organizationService.RetrieveMultiple(query);

                foreach (Entity listM in listMembers.Entities) {
                    memberGuids.Add(((EntityReference)listM.Attributes["entityid"]).Id);

                    QueryByAttribute newQuery = new QueryByAttribute("account");
                    newQuery.AddAttributeValue("accountid", ((EntityReference)listM.Attributes["entityid"]).Id);
                    newQuery.ColumnSet = new ColumnSet(true);
                    EntityCollection customersEntities = organizationService.RetrieveMultiple(newQuery);

                    foreach (Entity customer in customersEntities.Entities)
                    {
                        Account acc = new Account();
                        acc.naam = customer.Attributes["name"].ToString();
                        acc.id = customer.Attributes["accountid"].ToString();
                        acc.url = customer.Attributes["websiteurl"].ToString();
                        customerList.Add(acc);
                    }
                }                
            }
            return customerList;
        }

        public List<Info> getInfoByProductAndAccountId(string productId, string accountId)
        {
            List<Info> infoList = new List<Info>();
            if (userid != Guid.Empty)
            {
                QueryByAttribute query = new QueryByAttribute("scp_holoinfo");
                query.AddAttributeValue("scp_holoproduct", productId);
                query.AddAttributeValue("scp_holoaccount", accountId);
                query.ColumnSet = new ColumnSet(true);
                EntityCollection info = organizationService.RetrieveMultiple(query);

                foreach(Entity entity in info.Entities)
                {
                    Info holoinfo = new Info();

                    holoinfo.accountId = accountId;
                    holoinfo.productId = productId;
                    holoinfo.name = entity.Attributes["scp_name"].ToString();
                    holoinfo.infoType = ((OptionSetValue)entity.Attributes["scp_holoinfotype"]).Value.ToString();
                    holoinfo.infoUrl = entity.Attributes["scp_holoinfotypeurl"].ToString();

                    if(holoinfo.infoType == "798200000")
                    {
                        EntityReference docInfo = (EntityReference)entity.Attributes["scp_holoinfodocument"];
                        holoinfo.documentid = docInfo.Id.ToString();
                    }

                    infoList.Add(holoinfo);
                }
            }
            return infoList;
        }

        public Document getDocumentByInfoId(string infoId)
        {
            Document page = new Document();
            if (userid != Guid.Empty)
            {
                QueryByAttribute query = new QueryByAttribute("scp_holoinfodocument");
                query.AddAttributeValue("scp_holoinfodocumentid", infoId);
                query.ColumnSet = new ColumnSet(true);
                EntityCollection info = organizationService.RetrieveMultiple(query);

                foreach (Entity entity in info.Entities)
                {
                    page.pageUrl = new List<string>();
                    page.pageUrl.Add(entity.Attributes["scp_holoinfodocumentpage1"].ToString());
                    page.pageUrl.Add(entity.Attributes["scp_holoinfodocumentpage2"].ToString());
                    page.pageUrl.Add(entity.Attributes["scp_holoinfodocumentpage3"].ToString());
                    page.pageUrl.Add(entity.Attributes["scp_holoinfodocumentpage4"].ToString());
                    page.pageUrl.Add(entity.Attributes["scp_holoinfodocumentpage5"].ToString());
                }

            }
            return page;
        }
    }
}