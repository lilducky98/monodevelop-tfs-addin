//
// Microsoft.TeamFoundation.VersionControl.Client.Item
//
// Authors:
//	Joel Reed (joelwreed@gmail.com)
//  Ventsislav Mladenov (ventsislav.mladenov@gmail.com)
//
// Copyright (C) 2013 Joel Reed, Ventsislav Mladenov
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text;
using System.Xml.Linq;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace Microsoft.TeamFoundation.VersionControl.Client
{
    public sealed class Item: IEquatable<Item>, IComparable<Item>, IItem
    {
        private string downloadUrl;

        internal Item(VersionControlServer versionControlServer, string serverItem)
        {
            this.VersionControlServer = versionControlServer;
            this.ServerItem = serverItem;
        }

        public void DownloadFile(string localFileName)
        {
            if (ItemType == ItemType.Folder)
                return;

            Client.DownloadFile.WriteTo(localFileName, VersionControlServer.Repository,
                ArtifactUri);
        }
        //<Item cs="1" date="2006-12-15T16:16:26.95Z" enc="-3" type="Folder" itemid="1" item="$/" />
        //<Item cs="30884" date="2012-08-29T15:35:18.273Z" enc="65001" type="File" itemid="189452" item="$/.gitignore" hash="/S3KuHKFNtrxTG7LeQA7LQ==" len="387" />
        internal static Item FromXml(Repository repository, XElement element)
        {
            if (element == null)
                return null;
            string serverItem = element.Attribute("item").Value;
            Item item = new Item(repository.VersionControlServer, serverItem);

            if (element.Attribute("type") != null && !string.IsNullOrEmpty(element.Attribute("type").Value))
                item.ItemType = (ItemType)Enum.Parse(typeof(ItemType), element.Attribute("type").Value, true);

            if (element.Attribute("did") != null && !string.IsNullOrEmpty(element.Attribute("did").Value))
                item.DeletionId = Convert.ToInt32(element.Attribute("did").Value);


            item.CheckinDate = DateTime.Parse(element.Attribute("date").Value);
            item.ChangesetId = Convert.ToInt32(element.Attribute("cs").Value);
            item.ItemId = Convert.ToInt32(element.Attribute("itemid").Value);
            item.Encoding = Convert.ToInt32(element.Attribute("enc").Value);

            if (item.ItemType == ItemType.File)
            {
                item.ContentLength = Convert.ToInt32(element.Attribute("len").Value);
                if (element.Attribute("durl") != null)
                    item.downloadUrl = element.Attribute("durl").Value;

                if (element.Attribute("hash") != null && !string.IsNullOrEmpty(element.Attribute("hash").Value))
                    item.HashValue = Convert.FromBase64String(element.Attribute("hash").Value);
            }
            return item;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Item instance ");
            sb.Append(GetHashCode());

            sb.Append("\n	 ItemId: ");
            sb.Append(ItemId);

            sb.Append("\n	 CheckinDate: ");
            sb.Append(CheckinDate.ToString("s"));

            sb.Append("\n	 ChangesetId: ");
            sb.Append(ChangesetId);

            sb.Append("\n	 DeletionId: ");
            sb.Append(DeletionId);

            sb.Append("\n	 ItemType: ");
            sb.Append(ItemType);

            sb.Append("\n	 ServerItem: ");
            sb.Append(ServerItem);

            sb.Append("\n	 ContentLength: ");
            sb.Append(ContentLength);

            sb.Append("\n	 Download URL: ");
            sb.Append(downloadUrl);

            sb.Append("\n	 Hash: ");
            string hash = String.Empty;
            if (HashValue != null)
                hash = Convert.ToBase64String(HashValue);
            sb.Append(hash);

            return sb.ToString();
        }

        public int ContentLength { get; private set; }

        public ItemType ItemType { get; private set; }

        public DateTime CheckinDate { get; private set; }

        public int ChangesetId { get; private set; }

        public int DeletionId { get; private set; }

        public int Encoding { get; private set; }

        public int ItemId { get; private set; }

        public byte[] HashValue { get; private set; }

        public Uri ArtifactUri
        {
            get
            {
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    Item item = VersionControlServer.GetItem(ItemId, ChangesetId, true);
                    downloadUrl = item.downloadUrl;
                }

                return new Uri(String.Format("{0}?{1}", VersionControlServer.Repository.ItemUrl, downloadUrl));
            }
        }

        public string ServerItem { get; private set; }

        public VersionControlPath ServerPath { get { return ServerItem; } }

        public string ShortName
        {
            get
            {
                if (string.Equals(ServerItem, VersionControlPath.RootFolder))
                    return VersionControlPath.RootFolder;
                return ServerItem.Substring(ServerItem.LastIndexOf(VersionControlPath.Separator) + 1);
            }
        }

        public VersionControlServer VersionControlServer { get; private set; }

        #region Equal

        #region IComparable<Item> Members

        public int CompareTo(Item other)
        {
            return string.Compare(ServerItem, other.ServerItem, StringComparison.Ordinal);
        }

        #endregion

        #region IEquatable<Item> Members

        public bool Equals(Item other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return other.ServerItem == ServerItem;
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            Item cast = obj as Item;
            if (cast == null)
                return false;
            return Equals(cast);
        }

        public override int GetHashCode()
        {
            return ServerItem.GetHashCode();
        }

        public static bool operator ==(Item left, Item right)
        {
            return ReferenceEquals(null, left) ? ReferenceEquals(null, right) : left.Equals(right);
        }

        public static bool operator !=(Item left, Item right)
        {
            return !(left == right);
        }

        #endregion Equal

    }
}

