# StickyHomeworks gRPC-Web 接口文档

## 概述

本文档描述了StickyHomeworks Web应用程序提供的gRPC-Web接口。这些接口允许前端通过gRPC协议与后端服务进行通信，实现作业的增删改查操作。

## 技术细节

- 使用gRPC-Web作为通信协议
- 基于HTTP/1.1传输
- 支持所有现代浏览器
- 使用Protocol Buffers作为序列化格式

## 接口定义

### 服务名称
`homework.HomeworkService`

### 方法列表

#### GetAllHomeworks
获取所有作业列表
- 请求: `Empty`
- 回复: `HomeworkListReply`

#### GetHomework
根据ID获取特定作业
- 请求: `GetHomeworkRequest`
- 回复: `HomeworkReply`

#### CreateHomework
创建新作业
- 请求: `CreateHomeworkRequest`
- 回复: `HomeworkReply`

#### UpdateHomework
更新现有作业
- 请求: `UpdateHomeworkRequest`
- 回复: `HomeworkReply`

#### DeleteHomework
删除指定作业
- 请求: `DeleteHomeworkRequest`
- 回复: `OperationReply`

## 数据模型

### HomeworkModel
表示一个作业对象
- `int32 id` - 作业ID
- `string subject` - 科目
- `string tags` - 标签
- `string content` - 内容
- `string end_time` - 截止时间

## 使用示例

在JavaScript/TypeScript客户端中使用:

```typescript
// 创建客户端
const client = new HomeworkServiceClient('http://localhost:5000');

// 获取所有作业
client.getAllHomeworks(new Empty(), {}, (err, response) => {
  if (err) {
    console.error(err);
    return;
  }
  console.log(response.toObject());
});

// 创建作业
const request = new CreateHomeworkRequest();
request.setSubject('数学');
request.setContent('完成习题册第10页');
request.setTags('代数,方程');
client.createHomework(request, {}, (err, response) => {
  if (err) {
    console.error(err);
    return;
  }
  console.log(response.toObject());
});
```

## 错误处理

所有响应都包含一个布尔类型的`success`字段和字符串类型的`message`字段，用于指示操作是否成功以及相关的消息。

## 注意事项

1. 所有gRPC-Web请求都需要通过HTTP POST方法发送
2. Content-Type必须设置为application/grpc-web+proto
3. 需要在服务器端启用CORS（跨域资源共享）以支持浏览器直接调用
4. 时间格式遵循ISO 8601标准