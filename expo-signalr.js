/**
 * Sample React Native App
 * https://github.com/facebook/react-native
 *
 * @format
 * @flow strict-local
 */

import React from 'react';
import {
  H2,
  Text,
  Container,
  Header,
  Left,
  Body,
  Right,
  Title,
  Root,
  Content,
  List,
  ListItem,
} from 'native-base';

import {HubConnectionBuilder, LogLevel} from '@aspnet/signalr';
// export const URLAPI = 'http://localhost:5000/chatHub';
const URLAPI = 'http://192.168.1.2:9780/loginHub';


class App extends React.Component {
  constructor() {
    super();
    this.state = {
      orders: [""],
    };
  }
  _hubConnection = new HubConnectionBuilder()
    .withUrl(URLAPI)
    .configureLogging(LogLevel.Debug)
    .build();
  componentDidMount() {
    this._hubConnection.start().then(a => {
      //console.log('Connected server login');
      alert("login");
    });
    // this._hubConnection.on('ReceiveOrder', order => {
    //   let ordersState = this.state.orders;
    //   order.time = new Date().toLocaleTimeString();
    //   ordersState.push(order);
    //   this.setState({orders: ordersState});
    // });
    this._hubConnection.on('ReceiveLogin', order => {
      alert('Nhận tin nhắn');
      let ordersState = this.state.orders;
      //order.time = new Date().toLocaleTimeString();
      ordersState.push(order);
      this.setState({orders: ordersState});
    });
  }
  render() {
    return (
      <Root>
        <Container>
          <Header>
            <Left />
            <Body>
              <Title>React SignalR</Title>
            </Body>
            <Right />
          </Header>
          <Content>
            <List>
              {/* {this.state.orders.map(order => {
                return (
                  <ListItem key={order.id}>
                    <Body>
                      <Text>
                        Mesa:{' '}
                        <Text style={{fontWeight: 'bold'}}>
                          #{order.tableNumber}
                        </Text>
                      </Text>
                      <Text note>{order.item}</Text>
                      <Text note>Extras: {order.extras}</Text>
                    </Body>
                    <Right>
                      <Text note>{order.time}</Text>
                    </Right>
                  </ListItem>
                );
              })} */}

{this.state.orders.map((order,i) => {
                return (
                  <ListItem key={i}>
                    <Body>
                      <Text>
                        Mesa:{' '}
                        <Text style={{fontWeight: 'bold'}}>
                          #{order}
                        </Text>
                      </Text>

                    </Body>
                 
                  </ListItem>
                );
              })}
            </List>
          </Content>
        </Container>
      </Root>
    );
  }
}

export default App;
