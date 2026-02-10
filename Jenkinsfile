pipeline {
  agent any

  environment {
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
  }

  stages {
    stage('Checkout') {
      steps {
        echo 'Checking out source code from SCM...'
        checkout scm
      }
    }

    stage('Restore') {
      steps {
        echo 'Restoring NuGet packages...'
        script {
          if (isUnix()) {
            sh 'dotnet restore'
          } else {
            bat 'dotnet restore'
          }
        }
      }
    }

    stage('Build') {
      steps {
        echo 'Building the solution...'
        script {
          if (isUnix()) {
            sh 'dotnet build -c Release --no-restore'
          } else {
            bat 'dotnet build -c Release --no-restore'
          }
        }
      }
    }

    stage('Test') {
      steps {
        echo 'Running tests...'
        script {
          if (isUnix()) {
            sh 'dotnet test --no-build -c Release'
          } else {
            bat 'dotnet test --no-build -c Release'
          }
        }
      }
    }
  }

  post {
        success {
            echo 'Pipeline completed successfully!'
        }
        failure {
            echo 'Pipeline failed!'
        }
        always {
            archiveArtifacts artifacts: '**/bin/**', allowEmptyArchive: true
        }
    }
}
