# wbt-webjob
构造一个开放的.NET技术栈的WebJob，集成了Hangfire框架。
# Feautres
1. 通过api 建立webjob，job创建时根据jobtype 创建job id，jobtype可以指定内置job任务，也可以是定制化job ；创建的job，包含业务id，由调用方传入，可根据业务id查到job id。根据jobid 可以查到job的信息；
2. job执行后产生唯一日志id，根据job id或业务id可以查看业务执行的步骤和详情；
3. 自定义job，可根据web进行构建，可以配置http授权方法，以及http执行方法；
4. 支持websocket接口通讯，可以跟踪任务的进度，完成等事件；采用socket.io 类库。
