import PropTypes from 'prop-types';
import { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as signalR from '@aspnet/signalr/dist/browser/signalr.js';
import { repopulatePage } from 'Utilities/pagePopulator';
import { updateCommand, finishCommand } from 'Store/Actions/commandActions';
import { setAppValue, setVersion } from 'Store/Actions/appActions';
import { update, updateItem, removeItem } from 'Store/Actions/baseActions';
import { fetchHealth } from 'Store/Actions/systemActions';
import { fetchQueue, fetchQueueDetails } from 'Store/Actions/queueActions';

function isAppDisconnected(disconnectedTime) {
  if (!disconnectedTime) {
    return false;
  }

  return Math.floor(new Date().getTime() / 1000) - disconnectedTime > 180;
}

function createMapStateToProps() {
  return createSelector(
    (state) => state.app.isReconnecting,
    (state) => state.app.isDisconnected,
    (state) => state.queue.paged.isPopulated,
    (isReconnecting, isDisconnected, isQueuePopulated) => {
      return {
        isReconnecting,
        isDisconnected,
        isQueuePopulated
      };
    }
  );
}

const mapDispatchToProps = {
  updateCommand,
  finishCommand,
  setAppValue,
  setVersion,
  update,
  updateItem,
  removeItem,
  fetchHealth,
  fetchQueue,
  fetchQueueDetails
};

class SignalRConnector extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.signalRconnectionOptions = { transport: ['webSockets', 'longPolling'] };
    this.connection = null;
    this.retryInterval = 1;
    this.retryTimeoutId = null;
    this.disconnectedTime = null;
  }

  componentDidMount() {
    console.log('[signalR] starting');

    const url = `${window.Lidarr.urlBase}/signalr/messages`;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => {
          return window.Lidarr.apiKey;
        }
      })
      .build();

    this.connection.onclose(this.onClose);
    this.connection.on('receiveVersion', this.onReceiveVersion);
    this.connection.on('receiveMessage', this.onReceiveMessage);

    this.startConnection();
  }

  componentWillUnmount() {
    this.connection.stop();
    this.connection = null;
  }

  //
  // Control

  startConnection() {
    this.connection.start()
      .then(this.onConnected)
      .catch(this.onError);
  }

  retryConnection = () => {
    const attrs = {
      isReconnecting: true
    };

    if (isAppDisconnected(this.disconnectedTime)) {
      attrs.isConnected = false;
      attrs.isDisconnected = true;
    }

    setAppValue(attrs);

    this.retryTimeoutId = setTimeout(() => {
      this.startConnection();
      this.retryInterval = Math.min(this.retryInterval + 1, 10);
    }, this.retryInterval * 1000);
  }

  handleMessage = (message) => {
    const {
      name,
      body
    } = message;

    if (name === 'calendar') {
      this.handleCalendar(body);
      return;
    }

    if (name === 'command') {
      this.handleCommand(body);
      return;
    }

    if (name === 'album') {
      this.handleAlbum(body);
      return;
    }

    if (name === 'track') {
      this.handleTrack(body);
      return;
    }

    if (name === 'trackfile') {
      this.handleTrackFile(body);
      return;
    }

    if (name === 'health') {
      this.handleHealth(body);
      return;
    }

    if (name === 'artist') {
      this.handleArtist(body);
      return;
    }

    if (name === 'queue') {
      this.handleQueue(body);
      return;
    }

    if (name === 'queue/details') {
      this.handleQueueDetails(body);
      return;
    }

    if (name === 'queue/status') {
      this.handleQueueStatus(body);
      return;
    }

    if (name === 'wanted/cutoff') {
      this.handleWantedCutoff(body);
      return;
    }

    if (name === 'wanted/missing') {
      this.handleWantedMissing(body);
      return;
    }
  }

  handleCalendar = (body) => {
    if (body.action === 'updated') {
      this.props.updateItem({
        section: 'calendar',
        updateOnly: true,
        ...body.resource
      });
    }
  }

  handleCommand = (body) => {
    const resource = body.resource;
    const state = resource.state;

    // Both sucessful and failed commands need to be
    // completed, otherwise they spin until they timeout.

    if (state === 'completed' || state === 'failed') {
      this.props.finishCommand(resource);
    } else {
      this.props.updateCommand(resource);
    }
  }

  handleAlbum = (body) => {
    if (body.action === 'updated') {
      this.props.updateItem({
        section: 'albums',
        updateOnly: true,
        ...body.resource
      });
    }
  }

  handleTrack = (body) => {
    if (body.action === 'updated') {
      this.props.updateItem({
        section: 'tracks',
        updateOnly: true,
        ...body.resource
      });
    }
  }

  handleTrackFile = (body) => {
    const section = 'trackFiles';

    if (body.action === 'updated') {
      this.props.updateItem({ section, ...body.resource });
    } else if (body.action === 'deleted') {
      this.props.removeItem({ section, id: body.resource.id });
    }
  }

  handleHealth = (body) => {
    this.props.fetchHealth();
  }

  handleArtist = (body) => {
    const action = body.action;
    const section = 'artist';

    if (action === 'updated') {
      this.props.updateItem({ section, ...body.resource });
    } else if (action === 'deleted') {
      this.props.removeItem({ section, id: body.resource.id });
    }
  }

  handleQueue = (body) => {
    if (this.props.isQueuePopulated) {
      this.props.fetchQueue();
    }
  }

  handleQueueDetails = (body) => {
    this.props.fetchQueueDetails();
  }

  handleQueueStatus = (body) => {
    this.props.update({ section: 'queue.status', data: body.resource });
  }

  handleWantedCutoff = (body) => {
    if (body.action === 'updated') {
      this.props.updateItem({
        section: 'cutoffUnmet',
        updateOnly: true,
        ...body.resource
      });
    }
  }

  handleWantedMissing = (body) => {
    if (body.action === 'updated') {
      this.props.updateItem({
        section: 'missing',
        updateOnly: true,
        ...body.resource
      });
    }
  }

  //
  // Listeners

  onConnected = () => {
    console.debug('[signalR] connected');

    // Clear disconnected time
    this.disconnectedTime = null;

    // Repopulate the page (if a repopulator is set) to ensure things
    // are in sync after reconnecting.

    if (this.props.isReconnecting || this.props.isDisconnected) {
      repopulatePage();
    }

    this.props.setAppValue({
      isConnected: true,
      isReconnecting: false,
      isDisconnected: false,
      isRestarting: false
    });

    this.retryInterval = 5;

    if (this.retryTimeoutId) {
      this.retryTimeoutId = clearTimeout(this.retryTimeoutId);
    }
  }

  onReceiveVersion = (message) => {
    const version = message.body.version;

    this.props.setVersion({ version });
  }

  onReceiveMessage = (message) => {
    console.debug('[signalR] received', message.name, message.body);

    this.handleMessage(message);
  }

  onClose = () => {
    console.debug('[signalR] connection closed');

    if (window.Lidarr.unloading) {
      return;
    }

    if (!this.disconnectedTime) {
      this.disconnectedTime = Math.floor(new Date().getTime() / 1000);
    }

    this.retryConnection();
  }

  onError = () => {
    console.debug('[signalR] connection error');

    if (window.Lidarr.unloading) {
      return;
    }

    if (!this.disconnectedTime) {
      this.disconnectedTime = Math.floor(new Date().getTime() / 1000);
    }

    this.retryConnection();
  }

  //
  // Render

  render() {
    return null;
  }
}

SignalRConnector.propTypes = {
  isReconnecting: PropTypes.bool.isRequired,
  isDisconnected: PropTypes.bool.isRequired,
  isQueuePopulated: PropTypes.bool.isRequired,
  updateCommand: PropTypes.func.isRequired,
  finishCommand: PropTypes.func.isRequired,
  setAppValue: PropTypes.func.isRequired,
  setVersion: PropTypes.func.isRequired,
  update: PropTypes.func.isRequired,
  updateItem: PropTypes.func.isRequired,
  removeItem: PropTypes.func.isRequired,
  fetchHealth: PropTypes.func.isRequired,
  fetchQueue: PropTypes.func.isRequired,
  fetchQueueDetails: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SignalRConnector);
