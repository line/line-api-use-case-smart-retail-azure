using LineApiUseCaseSmartRetail.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LineApiUseCaseSmartRetail
{
    /// <summary>
    /// Functionsの基底クラス
    /// </summary>
    public abstract class BaseFunction
    {
        private static string DATABASE_NAME = "LineApiUseCaseSmartRetail";
        private static int ORDERS_CONTAINER_TTL = 24 * 60 * 60; // 24時間

        protected readonly CosmosClient client;

        protected BaseFunction(CosmosClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// couponIdでクーポン取得
        /// </summary>
        /// <param name="couponId">クーポンID</param>
        /// <returns>該当のクーポン一件</returns>
        protected async Task<Coupon> GetCouponByCouponIdAsync(string couponId)
        {
            if (string.IsNullOrWhiteSpace(couponId))
            {
                return null;
            }

            Coupon coupon;
            var container = await GetCouponsContainerAsync();
            using (var setIterator = container.GetItemLinqQueryable<Coupon>()
                      .Where(i => i.CouponId == couponId)
                      .ToFeedIterator())
            {
                var result = await setIterator.ReadNextAsync();
                coupon = result?.FirstOrDefault();
            }

            return coupon;
        }

        /// <summary>
        /// deletedが空のクーポンを全取得
        /// </summary>
        /// <returns>deletedが空のクーポン一覧</returns>
        protected async Task<IEnumerable<Coupon>> GetNotDeletedCouponsAsync()
        {
            var coupons = new List<Coupon>();
            var container = await GetCouponsContainerAsync();
            using (FeedIterator<Coupon> setIterator = container.GetItemLinqQueryable<Coupon>()
                      .Where(c => c.Deleted == null || c.Deleted == "")
                      .ToFeedIterator())
            {
                while (setIterator.HasMoreResults)
                {
                    coupons.AddRange(await setIterator.ReadNextAsync());
                }
            }

            return coupons;
        }

        /// <summary>
        /// barcodeで商品データ取得
        /// </summary>
        /// <param name="barcode">バーコード</param>
        /// <returns>該当の商品一件</returns>
        protected async Task<Item> GetItemByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }

            Item item;
            var container = await GetItemsContainerAsync();
            using (var setIterator = container.GetItemLinqQueryable<Item>()
                      .Where(i => i.Barcode == barcode)
                      .ToFeedIterator())
            {
                var result = await setIterator.ReadNextAsync();
                item = result?.FirstOrDefault();
            }

            return item;
        }

        /// <summary>
        /// idで注文データ取得
        /// </summary>
        /// <param name="id">id</param>
        /// <returns><see cref="Order"/></returns>
        protected async Task<Order> GetOrderAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var container = await GetOrdersContainerAsync();
            var order = await container.ReadItemAsync<Order>(id, new PartitionKey(id));

            return order;
        }

        /// <summary>
        /// userIdで注文データ取得
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <returns>該当の注文一覧</returns>
        protected async Task<IEnumerable<Order>> GetOrdersAsync(string userId)
        {
            var orders = new List<Order>();
            var container = await GetOrdersContainerAsync();
            using (FeedIterator<Order> setIterator = container.GetItemLinqQueryable<Order>()
                      .Where(o => o.UserId == userId)
                      .ToFeedIterator())
            {
                while (setIterator.HasMoreResults)
                {
                    orders.AddRange(await setIterator.ReadNextAsync());
                }
            }

            return orders;
        }

        /// <summary>
        /// userId且つorderIdで注文データ取得
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <param name="orderId">注文ID</param>
        /// <returns>該当の注文一覧</returns>
        protected async Task<IEnumerable<Order>> GetOrdersAsync(string userId, string orderId)
        {
            var orders = new List<Order>();
            var container = await GetOrdersContainerAsync();
            using (FeedIterator<Order> setIterator = container.GetItemLinqQueryable<Order>()
                      .Where(o => o.UserId == userId && o.OrderId == orderId)
                      .ToFeedIterator())
            {
                while (setIterator.HasMoreResults)
                {
                    orders.AddRange(await setIterator.ReadNextAsync());
                }
            }

            return orders;
        }

        /// <summary>
        /// 注文データの更新
        /// </summary>
        /// <param name="order">更新に使用する注文データ</param>
        /// <returns>更新後の注文データ</returns>
        protected async Task<Order> UpsertOrderAsync(Order order)
        {
            if (order == null)
            {
                return null;
            }

            var container = await GetOrdersContainerAsync();
            var itemResponse = await container.UpsertItemAsync(order, new PartitionKey(order.OrderId));

            return itemResponse;
        }

        /// <summary>
        /// LINEチャネルデータ取得
        /// </summary>
        /// <returns><see cref="LineChannel"/></returns>
        protected async Task<LineChannel> GetLineChannelAsync(string channelId)
        {
            LineChannel lineChannel;
            var container = await GetLineChannelContainerAsync();
            using (FeedIterator<LineChannel> setIterator = container.GetItemLinqQueryable<LineChannel>()
                      .Where(l => l.ChannelId == channelId)
                      .ToFeedIterator())
            {
                var result = await setIterator.ReadNextAsync();
                lineChannel = result?.FirstOrDefault();
            }

            return lineChannel;
        }

        /// <summary>
        /// LINEチャネルデータの更新
        /// </summary>
        /// <param name="lineChannel">更新に使用するLINEチャネルデータ</param>
        /// <returns>更新後のLINEチャネルデータ</returns>
        protected async Task<LineChannel> UpsertLineChannelAsync(LineChannel lineChannel)
        {
            if (lineChannel == null)
            {
                return null;
            }

            var container = await GetLineChannelContainerAsync();
            var itemResponse = await container.UpsertItemAsync(lineChannel, new PartitionKey(lineChannel.ChannelId));

            return itemResponse;
        }

        /// <summary>
        /// couponsコンテナ取得
        /// </summary>
        /// <returns>couponsコンテナ</returns>
        private async Task<Container> GetCouponsContainerAsync()
            => await GetContainerAsync("coupons", "/couponId");

        /// <summary>
        /// itemsコンテナ取得
        /// </summary>
        /// <returns>itemsコンテナ</returns>
        private async Task<Container> GetItemsContainerAsync()
            => await GetContainerAsync("items", "/barcode");

        /// <summary>
        /// ordersコンテナ取得
        /// </summary>
        /// <returns>ordersコンテナ</returns>
        private async Task<Container> GetOrdersContainerAsync()
        {
            Database database = await client.CreateDatabaseIfNotExistsAsync(DATABASE_NAME);

            // LINEユーザーIDを含むため、コンテナにTTLを設定
            var properties = new ContainerProperties
            {
                Id = "orders",
                PartitionKeyPath = "/orderId",
                DefaultTimeToLive = ORDERS_CONTAINER_TTL,
            };
            var container = await database.CreateContainerIfNotExistsAsync(properties);

            // コンテナが既にあり、TTLが設定されていなければ更新
            if (container.Resource.DefaultTimeToLive == null)
            {
                container.Resource.DefaultTimeToLive = ORDERS_CONTAINER_TTL;
                await client.GetContainer(DATABASE_NAME, "orders").ReplaceContainerAsync(container);
            }

            return container;
        }

        /// <summary>
        /// lineChannelコンテナ取得
        /// </summary>
        /// <returns>lineChannelコンテナ</returns>
        private async Task<Container> GetLineChannelContainerAsync()
            => await GetContainerAsync("lineChannel", "/channelId");

        /// <summary>
        /// コンテナを取得
        /// </summary>
        /// <param name="container">コンテナ名</param>
        /// <param name="partitionKey">パーティションキー名</param>
        /// <returns><see cref="Container"/></returns>
        private async Task<Container> GetContainerAsync(string container, string partitionKey)
        {
            Database database = await client.CreateDatabaseIfNotExistsAsync(DATABASE_NAME);
            return await database.CreateContainerIfNotExistsAsync(container, partitionKey);
        }
    }
}
